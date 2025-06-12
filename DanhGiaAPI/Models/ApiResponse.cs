namespace Tool_NhaAn.Models
{
    public class MealInfo
    {
        public double coM_DANGKY { get; set; }
        public double coM_THUCTE { get; set; }
    }
    public class ApiResponse
    {
        public string result { get; set; }
        public string? content { get; set; }
        public List<MealInfo> data { get; set; }
    }
}
