namespace RedPandaFlow.Application.DTOs
{
    public class UpdateColumnOrderRequest
    {
        public Guid ColumnId { get; set; }
        public int NewOrder { get; set; }
    }
}