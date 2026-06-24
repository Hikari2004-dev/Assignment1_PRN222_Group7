# Quy tắc tùy chỉnh dự án (Project Customization Rules)

## 1. Tự động Push
Sau khi hoàn thành bất kỳ thay đổi nào và kiểm tra hoạt động ổn định, luôn thực hiện commit bằng tiếng Việt và push thay đổi lên Git repository của dự án hiện tại.

## 2. Quy trình cấu hình tránh bị treo khi Push
Khi bắt đầu làm việc trên bất kỳ repository mới nào, AI phải kiểm tra và thiết lập phương thức push tối ưu theo các bước sau để tránh việc Git bị treo do Credential Manager cố gắng mở hộp thoại đăng nhập UI ẩn trong terminal chạy ngầm:
1. **Kiểm tra kết nối SSH**: Chạy `ssh -o StrictHostKeyChecking=no -T git@github.com` (hoặc git@gitlab.com tùy dịch vụ).
   - Nếu kết nối SSH thành công (trả về thông báo chào mừng của GitHub/GitLab), cập nhật remote URL sang SSH:
     ```bash
     git remote set-url origin git@github.com:<OWNER>/<REPO>.git
     ```
2. **Nếu phải dùng HTTPS**: 
   - Chạy `git remote -v` để xem URL hiện tại và `git config user.name` để xem username cấu hình.
   - Nếu URL chưa có username, hãy cập nhật lại remote URL chứa username (ví dụ `<USERNAME>@`) để Git tự động lấy thông tin xác thực đã lưu trong Credential Manager của hệ thống mà không yêu cầu tương tác UI:
     ```bash
     git remote set-url origin https://<USERNAME>@github.com/<OWNER>/<REPO>.git
     ```

## 3. Quy trình thực hiện đẩy code
```bash
git add .
git commit -m "nội dung commit ngắn gọn bằng tiếng Việt"
git push origin <tên_nhánh_hiện_tại>
```
