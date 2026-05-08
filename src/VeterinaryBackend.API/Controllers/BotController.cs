using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.Services;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.API.Controllers;

/// <summary>
/// Admin endpoints to read incoming Telegram messages and inspect subscribers.
/// All routes require the Admin role.
/// </summary>
[ApiController]
[Route("api/v1/Bot")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public class BotController : ControllerBase
{
    private readonly IBotMessageRepository _messages;
    private readonly IBotSubscriberRepository _subscribers;
    private readonly IUnitOfWork _uow;
    private readonly ITelegramBotService _bot;

    public BotController(
        IBotMessageRepository messages,
        IBotSubscriberRepository subscribers,
        IUnitOfWork uow,
        ITelegramBotService bot)
    {
        _messages = messages;
        _subscribers = subscribers;
        _uow = uow;
        _bot = bot;
    }

    public sealed class ReplyDto
    {
        public string Text { get; set; } = string.Empty;
    }

    public sealed class StatusDto
    {
        public BotMessageStatus Status { get; set; }
    }

    /// <summary>List incoming bot messages, newest first. ?onlyUnread=true to filter.</summary>
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] bool onlyUnread = false, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var rows = await _messages.GetAllAsync(onlyUnread, Math.Clamp(take, 1, 500), ct);
        return Ok(rows.Select(m => new
        {
            id = m.Id,
            chatId = m.ChatId,
            username = m.Username,
            firstName = m.FirstName,
            contactName = m.ContactName,
            phone = m.Phone,
            text = m.Text,
            isRead = m.IsRead,
            status = (int)m.Status,
            statusName = m.Status.ToString(),
            createdAt = m.CreatedAt
        }));
    }

    /// <summary>Update the lifecycle status of a request and notify the user via bot.</summary>
    [HttpPatch("messages/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusDto dto, CancellationToken ct)
    {
        var entity = await _messages.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        if (entity.Status == dto.Status)
            return NoContent();

        entity.Status = dto.Status;
        entity.IsRead = true;
        _messages.Update(entity);
        await _uow.SaveChangesAsync(ct);

        // Best-effort Telegram notification — don't fail the API call if the bot is offline.
        var label = dto.Status switch
        {
            BotMessageStatus.Pending    => "Qabul qilingan",
            BotMessageStatus.InProgress => "Jarayonda",
            BotMessageStatus.Completed  => "Muvaffaqiyatli yakunlandi",
            BotMessageStatus.Rejected   => "Rad etilgan",
            _ => dto.Status.ToString()
        };
        var icon = dto.Status switch
        {
            BotMessageStatus.Pending    => "📥",
            BotMessageStatus.InProgress => "⏳",
            BotMessageStatus.Completed  => "✅",
            BotMessageStatus.Rejected   => "❌",
            _ => "ℹ️"
        };
        try
        {
            await _bot.SendDirectAsync(entity.ChatId, $"{icon} Murojaatingiz holati yangilandi: {label}", ct);
        }
        catch { /* bot offline — admin status change still saved */ }

        return NoContent();
    }

    /// <summary>Send an admin reply through the bot to the user who wrote this message.</summary>
    [HttpPost("messages/{id:int}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return BadRequest(new { error = "Javob matni bo'sh bo'lishi mumkin emas." });

        var entity = await _messages.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        try
        {
            await _bot.SendDirectAsync(entity.ChatId, dto.Text.Trim(), ct);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "Telegram'ga yuborib bo'lmadi: " + ex.Message });
        }

        entity.IsRead = true;
        _messages.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Mark a single message as read.</summary>
    [HttpPatch("messages/{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var entity = await _messages.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.IsRead = true;
        _messages.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Delete a message permanently.</summary>
    [HttpDelete("messages/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _messages.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        _messages.Remove(entity);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Quick stats — unread count + active subscriber count.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        return Ok(new
        {
            unreadCount = await _messages.CountUnreadAsync(ct),
            subscriberCount = await _subscribers.CountActiveAsync(ct)
        });
    }
}
