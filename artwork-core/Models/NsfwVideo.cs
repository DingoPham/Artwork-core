namespace ArtworkCore.Models
{
    public class NsfwVideo
    {
        public string Id { get; set; }
        public string NsfwVideoUrl { get; set; }
        public string NsfwVideoName { get; set; } = "";
        public string NsfwVideoDescribe { get; set; } = "";
        public int Order { get; set; }
    }
}
