namespace WindowDeck
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using Mutex singleInstanceMutex = new(
                initiallyOwned: true,
                name: @"Local\WindowDeck",
                createdNew: out bool isFirstInstance);
            if (!isFirstInstance)
            {
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new WindowDeckApplicationContext());
        }
    }
}
