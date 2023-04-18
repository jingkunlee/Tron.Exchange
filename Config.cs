namespace Tron.Exchange;

[Serializable]
internal class Config {
    public string PrivateKey { get; set; }

    public string Address { get; set; }

    public decimal ExchangeRate { get; set; }
}