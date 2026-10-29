namespace RBBH.ConnectedParties.IoC.Extensions.Environment
{
    /// <summary>
    /// Contains methods to work with environments
    /// </summary>
    public static class EnvironmentExtensions
    {
        /// <summary>
        /// Returns true if current environment is not production env
        /// </summary>
        /// <param name="env">.NET builder environment interface</param>
        /// <returns>true if production. otherwise false.</returns>
        public static bool IsProduction(this IWebHostEnvironment env)
        {
            return !(env.IsDevelopment() || env.EnvironmentName.Equals("UAT"));
        }
    }
}
