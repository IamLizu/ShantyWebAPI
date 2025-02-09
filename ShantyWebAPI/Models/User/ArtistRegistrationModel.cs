﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShantyWebAPI.CustomAttributes;

namespace ShantyWebAPI.Models.User
{
    public class ArtistRegistrationModel
    {
        [FromHeader]
        [Required]
        public string JwtToken { get; set; }
        [Required]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9]{5,11}$", ErrorMessage = "Username Must be between 6-12 Characters and Must Not Contain Special Character")]
        [UsernameValidation]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        [EmailValidation]
        public string Email { get; set; }
        [Required]
        [PhoneValidation]
        public string Phone { get; set; }
        [Required]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "Password Must be 8 in Length and Must Contain Uppercase, Number and a Special Character")]
        public string Pass { get; set; }
        [Required]
        [ImageValidation]
        public IFormFile ProfileImage { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string FirstName { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string LastName { get; set; }
        [Required]
        [DobValidation]
        public string Dob { get; set; }
        [Required]
        public string Region { get; set; }
    }
}
