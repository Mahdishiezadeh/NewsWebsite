using System;
using System.Collections.Generic;
using System.Text;

namespace NewsWebsite.Common
{/// <summary>
/// هدف این متد تولید یک عدد تصادفی است
/// این عدد رندم بازگشتی شماره ردیف خبر یا ویدئو میتواند باشد
/// </summary>
    public static class CustomMethods
    {
        public static int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}
