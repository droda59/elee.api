using E133.Business;

namespace E133.Parser.LanguageUtilities
{
    internal interface IMeasureUnitDetector
    {
        MeasureUnit GetMeasureUnit(string measureUnit);
    }
}