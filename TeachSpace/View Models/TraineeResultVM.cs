namespace TeachSpace.View_Models
{
    public class TraineeResultVM
    {
        public int TraineeId { get; set; }
        public string TraineeName { get; set; }
        public string Image { get; set; }
        public int Degree { get; set; } // The grade
        public string Color => Degree >= 50 ? "success" : "danger"; // Helper for UI
    }
}