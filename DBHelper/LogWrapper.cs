using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DBHelper
{
    public static class LogWrapper
    {
        public static void Execute(Action action, string module, [CallerMemberName] string function = "")
        {
            SQLiteHelper.WriteLog(module, function, "Jarayon boshlandi");
            try
            {
                action();
                SQLiteHelper.WriteLog(module, function, "Jarayon muvaffaqiyatli yakunlandi");
            }
            catch (Exception ex)
            {
                //SQLiteHelper.WriteLog(module, function, $"Xatolik yuz berdi: {ex.Message}");
                SQLiteHelper.WriteError($"Module: {module}, Function: {function}, Xato: {ex.Message}");
                throw;
            }
        }

        public static async Task ExecuteAsync(Func<Task> action, string module, [CallerMemberName] string function = "")
        {
            SQLiteHelper.WriteLog(module, function, "Jarayon boshlandi");
            try
            {
                await action();
                SQLiteHelper.WriteLog(module, function, "Jarayon muvaffaqiyatli yakunlandi");
            }
            catch (Exception ex)
            {
                //SQLiteHelper.WriteLog(module, function, $"Xatolik yuz berdi: {ex.Message}");
                SQLiteHelper.WriteError($"Module: {module}, Function: {function}, Xato: {ex.Message}");
                throw;
            }
        }
    }

}
