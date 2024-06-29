using Xunit.Abstractions;

namespace OpenF1.Data.Tests;

public class DecompressUtilitiesUnitTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void DecompressCarData()
    {
        var carData =
            "vZfBbtswDIbfRee0IClSlHwt9gbbZcMOxVBgA4Ycut6CvHsdid7kiXFTA9sliZP8lkj+/Cifwofjy/OPp19h+nIKn16+hSkQEN+B3GH5iDxFnADvWYiJ5XM4hIfH5/nfp4CXl4fvj8fj08/6BYQpicRDoDAVPIQYpvmCwySHIGGC+fPl7Xyu/xi0GgnrL4i5irmKaVRHTy0xVXVuYqpiGLTsaAtCbisDdmJKgxrBkedCLWi8KH5HjTzKvZxJIdu5dOo4it2tM2lbu2hVp6pWZ+vJy1qBZJH3i49pw+ypVZa85U01eWlLALZ1bGmTTo2w0nt+QazlutxA+rzHMXbyHANVC1UJrWLAtjj3Yi/vKlis5tiZNeK4tnployKO4+KYuuhZJmfi1mbaqXEMnL29C0eqat2uuYhXNeKW9FT6pccmTV7OJaeW9kybS6uXNEVpUec4tPjaL9lLWsHFrdS79XKxWn2+wRYJVSmq4pskTGaQ0kfqWNNnmXUGQU/CkQlXSEg9T95FQs5lxQO6ZmufhNlQhhd//iFhuY2EKS47583p4ZNw6UiyrRsJna27JEyZLPK4g4RcPI6OBXNJKJrNmUi7SAhlucGahOME+gckTAYEJOrNOtbcJyGiDU9Im3b1QVi0Va30U58cs7sgRG6WUdwBQmQZQej0qA9C5VaxDDtACGh9grtAyNIqjrGPe6jYJgjpHuPcrpTfBqE1den7Sm8EIRXPW7eCEKTHSUvT6EuXJjmx2bLsAqEsBO+jxjFs/0iYCPpJd5VFPsNtxs5F60DoqF0OEpo5YNuZPgcxLd3cjz10jkUuCIVhBYP3gxAXEMb/DkKNCwi5NyvdBsJ5di6p4z0gtMm7AuGtJ0LW2Pos6Q4QRsu5rh6expT7IJyPhE29/QjhgjCpPW7q9kOfi8Gc7BCNcTWz/5afz1/Prw==";
        var result = DecompressUtilities.InflateBase64Data(carData);
        testOutputHelper.WriteLine(result);
        Assert.NotNull(result);
    }

    [Fact]
    public void DecompressPositionData()
    {
        var carData =
            "tdc9bxw3EAbg/7L1yZhPDuf61AlgFbGNFELgQggsB9a5Eu6/h7vH4W0Sm3Mq1Bgy4FekZ4YPuS/Lb1+fH0+PX5+W46eX5f7xy+fn08OXv5fjQkByB3qHfo9yZDwCvhMkZvaPy2H55en07fHz83J8WXD94/3p4fS9/XX59en+28Off7V/8vtyvEMUPSwf2k9SsR6Wj8sRocD5sNAkVSryJaUi0FPgLcWztdT7Wgo01lpTMkkpet+gEPeQSAshzFLFxw4ZI2ZrbFaOyoV6TKtssep1Tc22WMRpLGZ9MdI1VmZ7hDL2iPFfK2WN1UnMkfpqrealL9f6uPZsUhIqUnqs8mW1uhWEJp1GWKuwhqiybiE3XkOzRjN6T6HCZTysbjM1b7TVKAdFy8q2mP08xlsJ1pTDJWS+ZnjWZnXU0bAxVLhO4myHjDVi5NpjTi2mOhsqgL5FZfJLEXFNlVkRK7GMclxG0WFN2aQad62lMVNmvcu+jlSd1YOlT5SQx3FmP5/Ph9wcQ6Ra9TXmgPV6iNWovnpmjuGoB3tPbVM/N0chzIE4mqUm5kiE2nGmGA9KzVGHscMoo2JuDg1zqPSWeWoO07UcJXoGmTni5hGDsjd4bg6wDnNUbjbHYobR3Lo5kJmDht0c6432TcW3MGeMYiNY9gTPzeF+yCr6zebU63lm2l9kiTmx1sCjmSOZOeYSmDJ3PdBSczC4V+4n00FyczhmyvBmc0gDAaKoPJfbzPF2ZFRe9c4BGuZozL3WzJwi8H9zamoO9JaJ17gmimXmAIY5rPsrKTHnquIYD+XUHHKMWC290ZCagz7wGHtkyc3BccggXDTMzKluMsyJ6t9ijluYY5cXQXvVZeaMy5ZKCXPgrcyB8eyDur+S5uZAF9hKvHMoNcfsB+Zobk7MorLGDl1zc2KLynLZoxPk5sgArkAfRc3NGXNfDG41Z/RL0Pg6vzeYQ+/QiLCWV5jj8RQTozgtahk57RUxWha1J8rIEY0XnBfc32PTPu+efRFiT8nZRqmTEztUzcnRMr4ZdfeenZOz+0bicU1YSk4dzz72iBnl5CAMcrAEOZyRU+OrEU36VyNjRg4HA6T9Y8JV3oac9n06uAfcCzwjhwbAFks55uTAdX7/9eE9J6cJOBoWUm0Hek5OHaeFNciRlJz2m/8LFdSUHK6xxdLPc7uiUnKoBDk4nunUyPnj/A8=";
        var result = DecompressUtilities.InflateBase64Data(carData);
        testOutputHelper.WriteLine(result);
        Assert.NotNull(result);
    }
}
