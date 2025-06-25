namespace HarborManagementService.Models
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public int Code { get; set; }
        public T? Data { get; set; }

        public ApiResponse(string message, int code, T? data = default)
        {
            Message = message;
            Code = code;
            Data = data;
        }
    }
}
