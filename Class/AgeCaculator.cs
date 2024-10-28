namespace ArtworkCore.Class
{
    public class AgeCaculator
    {
        public int CalculateAge(DateTime ageClass)
        {
            var today = DateTime.Today;
            var age = today.Year - ageClass.Year;

            if (ageClass.Date > today.AddYears(-age)) // Kiểm tra nếu sinh nhật chưa qua trong năm nay
            {
                age--;
            }

            return age;
        }
    }
}
