# Lab 01: LINQ (Ngày 1)

Mở `LinqLab.slnx` bằng Visual Studio, nhấn **Ctrl+F5** để chạy.

## Dữ liệu giả (tự viết trong `Program.cs`)
- `record Department(int Id, string Name);`: 4 phòng, trong đó **1 phòng không có ai**.
- `record Employee(int Id, string FullName, string Email, int DepartmentId, decimal Salary, string Status, DateOnly HireDate);`:
  khoảng 15 người, `Status` gồm `Active`, `OnLeave`, `Resigned`; 1–2 người có `Email` rỗng.

## 20 câu LINQ
- [ ] 1. Danh sách nhân viên `Active`.
- [ ] 2. Nhân viên lương > 20 triệu, sắp xếp lương giảm dần.
- [ ] 3. Chỉ lấy `FullName` và `Salary` (projection bằng `Select`).
- [ ] 4. Tổng, trung bình, cao nhất, thấp nhất của lương.
- [ ] 5. Đếm số người theo `Status`.
- [ ] 6. Theo từng phòng ban: số người và lương trung bình.
- [ ] 7. Top 3 lương cao nhất công ty.
- [ ] 8. Top 2 lương cao nhất **mỗi** phòng ban.
- [ ] 9. Join nhân viên với phòng ban, in `FullName - DepartmentName`.
- [ ] 10. Left join: liệt kê **mọi** phòng ban kèm số nhân viên (phòng trống phải hiện 0).
- [ ] 11. `Any`: có ai lương dưới 8 triệu không? `All`: mọi người đều có email không?
- [ ] 12. `FirstOrDefault` với id không tồn tại, xử lý kết quả `null` cho an toàn.
- [ ] 13. Phân trang: trang 2, mỗi trang 5 người, sắp theo tên (`Skip`/`Take`).
- [ ] 14. Nhân viên vào làm trong năm 2024.
- [ ] 15. Người có lương **cao thứ 2** mỗi phòng ban (giống câu SQL `DENSE_RANK` trong tài liệu).
- [ ] 16. Danh sách tên phòng có ít nhất 1 người `Active` (không trùng lặp).
- [ ] 17. `ToDictionary` (Id → FullName) và `ToLookup` theo phòng ban.
- [ ] 18. Tìm người có họ "Nguyen", sắp xếp theo tên rồi theo họ.
- [ ] 19. Nhóm theo thâm niên: < 1 năm, 1–3 năm, > 3 năm.
- [ ] 20. **Deferred execution**: tạo query `Where` (có `Console.WriteLine` bên trong lambda), thêm 1 người vào
  list **sau khi** tạo query, rồi gọi `ToList()` hai lần. Giải thích bằng tiếng Anh vì sao kết quả như vậy.

## Tự kiểm tra
- Câu 10 có một phòng ban hiện 0 người.
- Nói được thành tiếng: *"What is deferred execution? IEnumerable vs IQueryable?"*
