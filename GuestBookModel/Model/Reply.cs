using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuestBookModel.Model
{
    [Table("Reply")]
    public partial class Reply
    {
        public int Id { get; set; }
        public string ReplyUserName { get; set; }
        public string ReplyMessage { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm:ss}")]
        public System.DateTime CreateDateTime { get; set; }
        public int GuestBookId { get; set; }
    }
}
