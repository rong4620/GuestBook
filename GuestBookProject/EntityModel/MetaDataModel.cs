using GuestBookProject.EntityModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GuestBookProject.EntityModel
{
    [MetadataType(typeof(ReplyMetadata))]
    public partial class Reply 
    {
        public class ReplyMetadata
        {
            [DataType(DataType.DateTime)]
            [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm:ss}")]
            public System.DateTime CreateDateTime { get; set; }

            [JsonIgnore]
            public virtual GuestBook GuestBook { get; set; }
        }
    }

    [MetadataType(typeof(GuestBookMetadata))]
    public partial class GuestBook
    {
        public class GuestBookMetadata
        {
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
        }
    }
}