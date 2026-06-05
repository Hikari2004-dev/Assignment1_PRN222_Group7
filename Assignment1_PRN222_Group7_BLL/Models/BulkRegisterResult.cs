using System.Collections.Generic;

namespace Assignment1_PRN222_Group7_BLL.Models
{
    public class BulkRegisterResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
