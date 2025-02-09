﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShantyWebAPI.CustomAttributes
{
    public class DobValidation:ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            bool IsValidDob(string dob)
            {
                DateTime today = DateTime.Today;
                int age = today.Year - Convert.ToDateTime(dob).Year;
                if (Convert.ToDateTime(dob) > today.AddYears(-age))
                    age--;
                if (age < 13)
                {
                    return false;
                }
                return true;
            }
            if(value != null)
            {
                if (!IsValidDob(value.ToString()))
                {
                    return new ValidationResult("User Must be At Least 13 Years Old ");
                }
            }
            return ValidationResult.Success;
        }
    }
}
