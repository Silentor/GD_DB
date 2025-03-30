using System.Collections.Generic;

namespace GDDB.Validations
{
    /// <summary>
    /// Added to GDObjects that need to be validated. This way validation code added to gd object code. There are other ways to validate gdobjects without adding validation code to gd object itself
    /// </summary>
    /// <param name="reports">to add error's reports</param>
    public interface IValidable
    {
        void Validate( GdFolder folder, GdDb db, List<ValidationReport> reports );
    }
}
