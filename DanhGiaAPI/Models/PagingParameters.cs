namespace DanhGiaAPI.Models
{
    public class PagingParameters
    {
        //const int maxPageSize = 50;

        //public int PageNumber { get; set; } = 1;

        //private int _pageSize = 10;
        //public int PageSize
        //{
        //    get => _pageSize;
        //    set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        //}
    }
    public class PagedResponse<T>
    {
        //public int PageNumber { get; set; }
        //public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public IEnumerable<T> Data { get; set; }

        public PagedResponse(IEnumerable<T> data, int count)
        {
            Data = data;
            TotalRecords = count;
            //PageNumber = pageNumber;
            //PageSize = pageSize;
        }
    }
    public class KetQuaFilterParameters : PagingParameters
    {
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        //public string? CodeDiemAn { get; set; }
        public string? CodeBuaAn { get; set; }
    }
}
