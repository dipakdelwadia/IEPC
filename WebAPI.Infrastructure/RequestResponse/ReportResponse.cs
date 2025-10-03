namespace WebAPI.RequestResponse
{

    public class ReportResponse<T> where T : class
    {
        private T? _model;

        public ReportResponse()
        {
           
        }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int PayloadCount { get; set; } = 0;
        public T? Payload
        {
            get => _model;
            set
            {
                _model = value;
            }
        }
    }
}
