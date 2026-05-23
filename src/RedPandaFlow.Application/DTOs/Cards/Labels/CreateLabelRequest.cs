namespace RedPandaFlow.Application.DTOs
{
    public class CreateLabelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // ex: "FF5733"
    }
}