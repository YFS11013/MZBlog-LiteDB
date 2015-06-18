﻿using LiteDB;
using MZBlog.Core.Documents;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MZBlog.Core.Commands.Accounts
{
    public class LoginCommand
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class LoginCommandResult : CommandResult
    {
        public LoginCommandResult()
            : base()
        { }

        public LoginCommandResult(string trrorMessage)
            : base(trrorMessage)
        { }

        public Author Author { get; set; }
    }

    public class LoginCommandInvoker : ICommandInvoker<LoginCommand, LoginCommandResult>
    {
        private readonly LiteDatabase _db;

        public LoginCommandInvoker(LiteDatabase db)
        {
            _db = db;
        }

        public LoginCommandResult Execute(LoginCommand loginCommand)
        {
            var hashedPassword = Hasher.GetMd5Hash(loginCommand.Password);

            var authorCol = _db.GetCollection<Author>(DBTableNames.Authors);
            if (!authorCol.Exists(Query.All()))
            {
                authorCol.Insert(new Author
                {
                    Email = "mz@bl.og",
                    DisplayName = "mzblog",
                    Roles = new[] { "admin" },
                    HashedPassword = Hasher.GetMd5Hash("mzblog")
                });
            }
            var author = authorCol.FindOne(x => x.Email == loginCommand.Email && x.HashedPassword == hashedPassword);

            if (author != null)
                return new LoginCommandResult() { Author = author };

            return new LoginCommandResult(trrorMessage: "用户名或密码不正确") { };
        }
    }
}