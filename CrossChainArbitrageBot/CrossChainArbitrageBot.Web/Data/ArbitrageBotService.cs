namespace CrossChainArbitrageBot.Web.Data;

public class ArbitrageBotService
{
    private WebViewViewModel? viewModel;
    private int initialized = 0;
    
    public void Initialize()
    {
        if (Interlocked.Exchange(ref initialized, 1) == 0)
        {
            //Run startup logic
            viewModel = new WebViewViewModel();
        }
    }
}