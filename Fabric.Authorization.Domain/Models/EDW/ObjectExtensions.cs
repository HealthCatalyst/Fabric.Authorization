using System;
public static class ObjectExtensions
{
    public static void CheckWhetherArgumentIsNull(this object argument, string argumentName)
    {
        if (argument == null)
        {
            throw new ArgumentNullException(argumentName);
        }
    }
}