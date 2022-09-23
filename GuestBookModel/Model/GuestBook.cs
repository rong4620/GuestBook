using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuestBookModel.Model
{
    [Table("GuestBook")]
    public partial class GuestBook
    {  
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        [DisplayName("留言者")]
        public string UserName { get; set; }
        [Required]
        [StringLength(100)]
        [DisplayName("主題")]
        public string Title { get; set; }
        [Required]
        [DisplayName("內容")]
        public string Message { get; set; }

        [DisplayName("建立日期")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm:ss}")]
        public System.DateTime CreateDateTime { get; set; }
        public Nullable<int> UserId { get; set; }

        [Write(false)]
        public int ReplyCount { get; set; }

        [Write(false)]     
        public ICollection<Reply> Reply { get; set; }
    }

}
