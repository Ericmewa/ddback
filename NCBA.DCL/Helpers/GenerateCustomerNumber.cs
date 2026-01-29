// private async Task<string> GenerateUniqueCustomerNumber()
// {
//     string number;
//     do
//     {
//         number = GenerateCustomerNumber();
//     } while (await _context.Users.AnyAsync(u => u.CustomerNumber == number && u.Role == UserRole.Customer));

//     return number;
// }
