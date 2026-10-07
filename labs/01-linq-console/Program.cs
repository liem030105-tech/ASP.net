// Ngày 1 – LINQ lab. Xem đề bài đầy đủ trong labs/01-linq-console/README.md.

// ===== Dữ liệu mẫu =====
var departments = new List<Department>
{
    new(1, "Engineering"),
    new(2, "HR"),
    new(3, "Finance"),
    new(4, "Marketing"),   // phòng không có nhân viên (dùng cho câu 10)
};

var employees = new List<Employee>
{
    new(1,  "Nguyen Van An",    "an@demo.local",    1, 18_000_000m, "Active",   new DateOnly(2023, 3, 1)),
    new(2,  "Tran Thi Binh",    "binh@demo.local",  1, 35_000_000m, "Active",   new DateOnly(2020, 6, 15)),
    new(3,  "Le Van Cuong",     "cuong@demo.local", 2, 22_000_000m, "Active",   new DateOnly(2021, 1, 10)),
    new(4,  "Pham Minh Dung",   "dung@demo.local",  1, 25_000_000m, "OnLeave",  new DateOnly(2022, 9, 5)),
    new(5,  "Vo Thu Giang",     "",                 1, 12_000_000m, "Active",   new DateOnly(2024, 2, 20)),
    new(6,  "Dang Thi Hoa",     "hoa@demo.local",   2, 15_000_000m, "Resigned", new DateOnly(2019, 11, 1)),
    new(7,  "Bui Dang Khoa",    "khoa@demo.local",  3, 28_000_000m, "Active",   new DateOnly(2018, 4, 12)),
    new(8,  "Nguyen Thi Lan",   "lan@demo.local",   3, 19_000_000m, "Active",   new DateOnly(2024, 7, 1)),
    new(9,  "Hoang Van Minh",   "minh@demo.local",  1, 40_000_000m, "Active",   new DateOnly(2017, 8, 21)),
    new(10, "Do Thi Ngoc",      "",                 2, 7_500_000m,  "Active",   new DateOnly(2025, 5, 3)),
    new(11, "Nguyen Huu Phuc",  "phuc@demo.local",  3, 32_000_000m, "OnLeave",  new DateOnly(2020, 2, 14)),
    new(12, "Truong Van Quang", "quang@demo.local", 1, 21_000_000m, "Active",   new DateOnly(2023, 10, 9)),
    new(13, "Ly Thi Thao",      "thao@demo.local",  3, 16_500_000m, "Resigned", new DateOnly(2021, 12, 1)),
    new(14, "Phan Van Tuan",    "tuan@demo.local",  2, 26_000_000m, "Active",   new DateOnly(2024, 1, 15)),
    new(15, "Mai Thi Uyen",     "uyen@demo.local",  1, 30_000_000m, "Active",   new DateOnly(2022, 3, 28)),
};

// ===== Q1. Nhân viên Active =====
var activeEmployees = employees.Where(e => e.Status == "Active");
Print("Q1. Active employees", activeEmployees);

// ===== Q2. Lương > 20 triệu, giảm dần =====
var highEarners = employees
    .Where(e => e.Salary > 20_000_000m)
    .OrderByDescending(e => e.Salary);
Print("Q2. Salary > 20M, highest first", highEarners);

// ===== Q3. Chỉ lấy FullName và Salary =====
var nameAndSalary = employees.Select(e => new { e.FullName, e.Salary });
Print("Q3. Name and salary only", nameAndSalary);

// ===== Q4. Tổng, trung bình, max, min =====
Console.WriteLine();
Console.WriteLine("Q4. Salary statistics");
Console.WriteLine($"  Total:   {employees.Sum(e => e.Salary):N0}");
Console.WriteLine($"  Average: {employees.Average(e => e.Salary):N0}");
Console.WriteLine($"  Max:     {employees.Max(e => e.Salary):N0}");
Console.WriteLine($"  Min:     {employees.Min(e => e.Salary):N0}");

// ===== Q5. Đếm theo Status =====
var countByStatus = employees
    .GroupBy(e => e.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() });
Print("Q5. Employees per status", countByStatus);

// ===== Q6. Theo phòng ban: số người và lương trung bình =====
var statsByDepartment = employees
    .GroupBy(e => e.DepartmentId)
    .Select(g => new
    {
        DepartmentId = g.Key,
        Count = g.Count(),
        AverageSalary = Math.Round(g.Average(e => e.Salary)),
    });
Print("Q6. Headcount and average salary per department", statsByDepartment);

// ===== Q7. Top 3 lương cao nhất công ty =====
var top3 = employees
    .OrderByDescending(e => e.Salary)
    .Take(3)
    .Select(e => new { e.FullName, e.Salary });
Print("Q7. Top 3 salaries", top3);

// ===== Q8. Top 2 lương cao nhất mỗi phòng ban =====
var top2PerDepartment = employees
    .GroupBy(e => e.DepartmentId)
    .SelectMany(g => g.OrderByDescending(e => e.Salary).Take(2))
    .Select(e => new { e.DepartmentId, e.FullName, e.Salary });
Print("Q8. Top 2 salaries per department", top2PerDepartment);

// ===== Q9. Join nhân viên với phòng ban =====
var employeeWithDepartment = employees.Join(
    departments,
    e => e.DepartmentId,
    d => d.Id,
    (e, d) => $"{e.FullName} - {d.Name}");
Print("Q9. Employee - Department", employeeWithDepartment);

// ===== Q10. Left join: mọi phòng ban kèm số nhân viên =====
var headcountPerDepartment =
    from d in departments
    join e in employees on d.Id equals e.DepartmentId into deptEmployees
    select new { Department = d.Name, EmployeeCount = deptEmployees.Count() };
Print("Q10. Every department with headcount (left join)", headcountPerDepartment);

// ===== Q11. Any / All =====
var anyBelow8M = employees.Any(e => e.Salary < 8_000_000m);
var allHaveEmail = employees.All(e => !string.IsNullOrWhiteSpace(e.Email));
Console.WriteLine();
Console.WriteLine("Q11. Any / All");
Console.WriteLine($"  Anyone earning < 8M?   {anyBelow8M}");
Console.WriteLine($"  Everyone has an email? {allHaveEmail}");

// ===== Q12. FirstOrDefault với id không tồn tại =====
var missingEmployee = employees.FirstOrDefault(e => e.Id == 99);
var existingEmployee = employees.FirstOrDefault(e => e.Id == 3);
Console.WriteLine();
Console.WriteLine("Q12. FirstOrDefault and null handling");
Console.WriteLine($"  Employee 99: {missingEmployee?.FullName ?? "(not found)"}");
Console.WriteLine($"  Employee 3:  {existingEmployee?.FullName ?? "(not found)"}");

// ===== Q13. Phân trang: trang 2, mỗi trang 5 người, sắp theo tên =====
const int page = 2;
const int pageSize = 5;
var secondPage = employees
    .OrderBy(e => e.FullName)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(e => e.FullName);
Print($"Q13. Page {page} (page size {pageSize}), ordered by name", secondPage);

// ===== Q14. Nhân viên vào làm trong năm 2024 =====
// Lọc theo khoảng ngày (giống điều kiện SARGable trong SQL);
// viết e.HireDate.Year == 2024 cũng cho cùng kết quả.
var hiredIn2024 = employees
    .Where(e => e.HireDate >= new DateOnly(2024, 1, 1) && e.HireDate < new DateOnly(2025, 1, 1))
    .Select(e => new { e.FullName, e.HireDate });
Print("Q14. Hired in 2024", hiredIn2024);

// ===== Q15. Lương cao thứ 2 mỗi phòng ban (giống DENSE_RANK() = 2) =====
var secondHighestPerDepartment = employees
    .GroupBy(e => e.DepartmentId)
    .SelectMany(g =>
    {
        var secondSalary = g.Select(e => e.Salary)
            .Distinct()
            .OrderByDescending(s => s)
            .Skip(1)
            .Cast<decimal?>()
            .FirstOrDefault();   // null nếu phòng chỉ có 1 mức lương
        return g.Where(e => e.Salary == secondSalary);
    })
    .Select(e => new { e.DepartmentId, e.FullName, e.Salary });
Print("Q15. Second highest salary per department", secondHighestPerDepartment);

// Q16–Q20: tự làm tiếp (xem README.md)

// ===== Hàm in dùng chung =====
void Print<T>(string title, IEnumerable<T> items)
{
    Console.WriteLine();
    Console.WriteLine(title);
    foreach (var item in items)
        Console.WriteLine($"  {item}");
}

// ===== Kiểu dữ liệu (phải nằm cuối file) =====
record Department(int Id, string Name);
record Employee(int Id, string FullName, string Email, int DepartmentId,
                decimal Salary, string Status, DateOnly HireDate);
