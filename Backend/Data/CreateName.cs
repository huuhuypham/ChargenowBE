using Backend.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Backend.Data
{
    // Lớp này dùng để ánh xạ với các key trong file JSON
    public class NameRecordFromJson
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }
    }

    // Phương thức mở rộng để xóa dấu tiếng Việt
    public static class StringHelper
    {
        public static string RemoveDiacritics(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC).Replace("Đ", "D").Replace("đ", "d");
        }
    }

    public static class CreateName
    {
        private static readonly string VietnameseNameDbUrl = "https://raw.githubusercontent.com/duyet/vietnamese-namedb-crawler/master/data/json/data.json";
        private static readonly Random _random = new Random();

        // Sửa lại phương thức để nhận DbContext và không trả về List<User> nữa
        // Trong file Data/CreateName.cs
        public static async Task<List<User>> GenerateUserListOnlyAsync(int count = 20)
        {
            try
            {
                using var httpClient = new HttpClient();
                string jsonString = await httpClient.GetStringAsync(VietnameseNameDbUrl);
                var nameRecords = JsonSerializer.Deserialize<List<NameRecordFromJson>>(jsonString);

                if (nameRecords == null || !nameRecords.Any()) return new List<User>();

                var selectedRecords = nameRecords
                                        .Where(r => !string.IsNullOrWhiteSpace(r.FullName))
                                        .Take(count)
                                        .ToList();

                List<User> userList = selectedRecords.Select(record =>
                {
                    string fullName = record.FullName!.Trim();
                    var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string firstName = parts.LastOrDefault() ?? "";
                    string lastNameAndMiddle = string.Join(" ", parts.Take(parts.Length - 1));

                    string firstNameNoDiacritics = firstName.RemoveDiacritics();
                    string lastNameAndMiddleNoDiacritics = lastNameAndMiddle.RemoveDiacritics();

                    string baseUsername = $"{firstNameNoDiacritics.ToLower()}.{lastNameAndMiddleNoDiacritics.Replace(" ", "").ToLower()}";
                    string username = $"{baseUsername}{_random.Next(10, 100)}";
                    DateTime endAgeDate = DateTime.Today.AddYears(-18);
                    DateTime startAgeDate = DateTime.Today.AddYears(-60);
                    int totalDaysInSpan = (int)(endAgeDate - startAgeDate).TotalDays;
                    DateTime dateOfBirth = startAgeDate.AddDays(_random.Next(totalDaysInSpan));
                    dateOfBirth = DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc);
                    return new User
                    {
                        FullName = fullName,
                        Username = username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                        PhoneNumber = "0" + _random.NextInt64(100_000_000, 1_000_000_000).ToString(),
                        DateOfBirth = dateOfBirth,
                        Email = $"{username}@example.com",
                        Balance = _random.Next(100_000, 10_000_000),
                        Role = UserRole.User
                    };
                }).ToList();

                return userList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo danh sách User: {ex.Message}");
                return new List<User>();
            }
        }
    }
}