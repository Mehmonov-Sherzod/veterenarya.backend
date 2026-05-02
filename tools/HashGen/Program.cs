var password = args.Length > 0 ? args[0] : "Admin@123";
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
Console.WriteLine(hash);
