namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Bảng liên kết Many-to-Many giữa Giảng viên (User) và Môn học (Subject).
    /// Giảng viên chỉ được thao tác trên các môn học được phân quyền trong bảng này.
    /// </summary>
    public class LecturerSubject
    {
        public int Id { get; set; }

        public int LecturerId { get; set; }

        public int SubjectId { get; set; }

        // Navigation
        public User Lecturer { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
    }
}
