using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers
{

    //localhost:xxxxx/api/users
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        public UserController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var allUsers = dbContext.Users.ToList();

            return Ok(allUsers);
        }


        [HttpGet]
        [Route("{UserId:int}")]
        public IActionResult GetUserbyId(int UserId)
        {

            var user = dbContext.Users.Find(UserId);

            if (user is null)
            {
                return NotFound();

            }
            else
            {
                return Ok(user);
            }
        }


        [HttpPost]

        public IActionResult AddUser(adduserDto addUserDto)
        {

            var userEntity = new Users()
            {
                Email = addUserDto.Email,
                Name = addUserDto.Name,
                Password = addUserDto.Password,
                Username = addUserDto.Username

            };


            dbContext.Users.Add(userEntity);
            dbContext.SaveChanges();
            return Ok(userEntity);
        }


        [HttpPut]
        [Route("{UserId:int}")]
        public IActionResult UpdateUser(int UserId, UserUpdateDto userUpdateDto)
        {

            var user = dbContext.Users.Find(UserId);
            if (user is null)
            {
                return NotFound();
            }
            user.Name = userUpdateDto.Name;
            user.Email = userUpdateDto.Email;
            user.Password = userUpdateDto.Password;
            user.Username = userUpdateDto.Username;
            dbContext.SaveChanges();
            
            return Ok(user);
        }

        [HttpDelete]
        [Route("{UserId:int}")]

        public IActionResult DeleteUser(int UserId)
        {

            var user = dbContext.Users.Find(UserId);

            if (user is null)
            {

                return NotFound();
            }

            dbContext.Users.Remove(user);
            dbContext.SaveChanges();
            return Ok();  


        }

      

    }   

}