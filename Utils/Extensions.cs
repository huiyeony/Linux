using System;
using Server.DB;

namespace Server.Util
{
    public static class Extensions
    {
        public static bool SaveChangesEx(this AppDbContext db)
        {
            try
            {
                db.SaveChanges();
                return true;//성공하면 true 
            }
            catch
            {
                return false;
            }
        }
    }
}

