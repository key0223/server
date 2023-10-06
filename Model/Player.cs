using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
 * PK (Primary Key) - 기본 키 (단일 값 (Unique) null이 될 수 없음
 * NP (Navigation Property) - 다른 클래스를 가르키는 key
 * FK (Foreign key) -  한 테이블의 컬럼이 다른 테이블의 PK
*/
namespace addkeyserver.DTO
{
    public class UserDb
    {
        public int UserDbId { get; set; } // PK

        public string UserEmail { get; set; }
        public string UserPassword { get; set; }
    }

    public class PlayerDb
    {
        public int PlayerDbId { get; set; } //PK

        public int OwnerId { get; set; }
        public UserDb Owner { get; set; } //FK (NP)

        public string PlayerName { get; set; }
        public int Gold { get; set; }
        public int Gem { get; set; }
        public int Health { get; set; }

    }
}
