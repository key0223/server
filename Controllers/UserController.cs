using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using addkeyserver.Database;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using IDatabase = StackExchange.Redis.IDatabase;
using Newtonsoft.Json;
using static addkeyserver.DTO.DTO;
using addkeyserver.DTO;

namespace addkeyserver.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        private readonly IDatabase _redis;
        public UserController(AppDbContext context, IConfiguration configuration,IDatabase redis)
        {
            _context = context;
            _configuration = configuration;
            _redis = redis;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {

            var user = await _context.Users.Where(u => u.UserEmail == request.Email).FirstOrDefaultAsync();

            if (user == null)
            {
                return BadRequest(new LoginResponse { Message = "0" });
            }
            if (user.UserPassword != request.Password)
            {
                return BadRequest(new LoginResponse { Message = "계정 또는 비밀번호를 확인해주세요." });
            }

            var player = await _context.Players.Where(p => p.OwnerId == user.UserDbId).FirstOrDefaultAsync();

            if (player == null)
            {
                return BadRequest(new LoginResponse { Message = "0" });
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.UserDbId.ToString()),
                new Claim("UserDbId", user.UserDbId.ToString())
            };
            
            //Jwt 토큰 생성
            var token = new JwtSecurityToken
                (
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),    // 임시 1일
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    SecurityAlgorithms.HmacSha256)
                ) ;

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var playerInfo = new PlayerInfo
            {
                PlayerDbId = player.PlayerDbId,
                PlayerName = player.PlayerName,
                Token = tokenString,
                Gold = player.Gold,
                Gem = player.Gem,
                Health = player.Health
            };

            return Ok(new LoginResponse
            {
                Message = "1",
                Player = playerInfo,
            });

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = await _context.Users.Where(u => u.UserEmail == request.Email).FirstOrDefaultAsync();
            var player = await _context.Players.Where(p => p.PlayerName == request.PlayerName).FirstOrDefaultAsync();

            if (user != null)
            {
                if (player != null)
                {
                    return BadRequest(new RegisterResponse { Success = false, Message = "이미 등록된 닉네임" });
                }

                // 새 캐릭터 생성
                var newPlayer = new PlayerDb
                {
                    OwnerId = user.UserDbId,
                    PlayerName = request.PlayerName,
                    Gold = 0,
                    Gem = 0,
                    Health = 100,
                };

                return BadRequest(new RegisterResponse { Success = true, Message = "완료" });
            }
            else
            {
                // 새 계정 생성
                var newUser = new UserDb()
                {
                    UserEmail = request.Email,
                    UserPassword = request.Password,
                };

                // 저장
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                if (player != null)
                {
                    return BadRequest(new RegisterResponse { Success = false, Message = "이미 등록된 닉네임" });
                }

                // 새 캐릭터 생성
                var newPlayer = new PlayerDb
                {
                    OwnerId = newUser.UserDbId,
                    PlayerName = request.PlayerName,
                    Gold = 0,
                    Gem = 0,
                    Health = 100,
                };


                // 저장
                _context.Players.Add(newPlayer);
                await _context.SaveChangesAsync();
                return Ok(new RegisterResponse { Success = true, Message = "완료" });

            }
        }

        [HttpPost("save")]
        [Authorize]
        public async Task<IActionResult> SavePlayerData([FromBody] PlayerInfo player)
        {

            var userDbId = int.Parse(HttpContext.User.Claims.FirstOrDefault(u => u.Type == "UserDbId").Value);

            var playerDb = await _context.Players.Where(p => p.OwnerId == userDbId).FirstOrDefaultAsync();
            if(playerDb == null)
            {
                return Ok(new RegisterResponse { Success = true, Message = "저장 실패" });
            }

            playerDb.Gold = player.Gold;
            playerDb.Gem = player.Gem;
            playerDb.Health = player.Health;

            await _context.SaveChangesAsync();
            return Ok(new RegisterResponse { Success = true, Message = "저장 완료" });

            /*
            var existingPlayer = await _context.Players.Where(p => p.PlayerName == player.PlayerName).FirstOrDefaultAsync();

            if (existingPlayer != null)
            {
                // 기존 데이터가 있으면 업데이트
                existingPlayer.Gold = player.Gold;
                existingPlayer.Gem = player.Gem;
                existingPlayer.Health = player.Health;
            }
            else
            {
                // 새로운 데이터면 추가
                var newPlayer = new PlayerDb
                {
                    PlayerName = player.PlayerName,
                    Gold = player.Gold,
                    Gem = player.Gem,
                    Health = player.Health,
                };
                _context.Players.Add(newPlayer);
            }

            await _context.SaveChangesAsync();
            return Ok(new RegisterResponse { Success = true, Message = "저장 완료" });
            */
        }

        [HttpGet]
        public IActionResult Redis()
        {
            _redis.StringSet("test", "testValue");
            var result = _redis.StringGet("test").ToString();

            return Ok(result);
        }

        [HttpPost("redisLogin")]
        public async Task<LoginResponse> RedisLogin([FromBody] LoginRequest request)
        {
            var user = await _context.Users.Where(u => u.UserEmail == request.Email).FirstOrDefaultAsync();
            var redisPlayerInfo = new RedisPlayerInfo
            {
                UserDbId = user.UserDbId
            };

            var jsonString = JsonConvert.SerializeObject(redisPlayerInfo);
            // key: UserEmail , Value UserDbId가 저장된 RedisPlayerInfo Obj
            await _redis.StringSetAsync(user.UserEmail, jsonString, new TimeSpan(0, 0, 30)); // 30초 후에 redis에서 삭제

            var player = await _context.Players.Where(p => p.OwnerId == user.UserDbId).FirstOrDefaultAsync();

            var playerInfo = new PlayerInfo
            {
                PlayerDbId = player.PlayerDbId,
                PlayerName = player.PlayerName,
                Token = user.UserEmail,
                Gold = player.Gold,
                Gem = player.Gem,
                Health = player.Health
            };

            var response = new LoginResponse
            {
                Message = "1",
                Player = playerInfo,
            };

            return response;

        }
    }
}
