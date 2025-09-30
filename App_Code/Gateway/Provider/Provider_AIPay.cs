using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



/// <summary>
/// Provider_AsiaPay 的摘要描述
/// </summary>
public class Provider_AIPay : GatewayCommon.ProviderGateway, GatewayCommon.ProviderGatewayByWithdraw
{

    public Provider_AIPay()
    {
        //初始化設定檔資料
        SettingData = GatewayCommon.GetProverderSettingData("AIPay");
    }

    Dictionary<string, string> GatewayCommon.ProviderGateway.GetSubmitData(GatewayCommon.Payment payment)
    {
        Dictionary<string, string> dataDic = new Dictionary<string, string>();
        return dataDic;
    }

    string GatewayCommon.ProviderGateway.GetCompleteUrl(GatewayCommon.Payment payment)
    {
        string sign;
        string signStr = "";
        string tradeTypeValue = SettingData.ServiceSettings.Find(x => x.ServiceType == payment.ServiceType).TradeType;
        Dictionary<string, object> signDic = new Dictionary<string, object>();

        // 生成一个随机的 GUID
        Guid guid = Guid.NewGuid();
        // 将 GUID 转换成字符串
        string randomString = guid.ToString("N");
        // 获取当前时间的 DateTimeOffset 对象
        DateTimeOffset currentTime = DateTimeOffset.Now;
        // 将当前时间转换为时间戳（毫秒级）
        long timestamp = currentTime.ToUnixTimeMilliseconds();


        signDic.Add("mchKey", SettingData.MerchantCode);//
        signDic.Add("product", tradeTypeValue);
        signDic.Add("mchOrderNo", payment.PaymentSerial);//
        signDic.Add("amount", (payment.OrderAmount * 100).ToString("#.##"));//
        signDic.Add("nonce", randomString);//
        signDic.Add("timestamp", timestamp);//
        signDic.Add("notifyUrl", SettingData.NotifyAsyncUrl);//

        signDic = CodingControl.AsciiDictionary2(signDic);

        foreach (KeyValuePair<string, object> item in signDic)
        {
            signStr += item.Key + "=" + item.Value + "&";
        }
        signStr = signStr.Substring(0, signStr.Length - 1);
        signStr = signStr + SettingData.MerchantKey;

        sign = CodingControl.GetMD5(signStr, false);

        signDic.Add("sign", sign);
     
        PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 1, payment.PaymentSerial, payment.ProviderCode);
        var jsonStr = GatewayCommon.RequestJsonAPI(SettingData.ProviderUrl, JsonConvert.SerializeObject(signDic), payment.PaymentSerial, payment.ProviderCode);

        //This line executes whether or not the exception occurs.
        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["code"].ToString() == "200")
                {
                    PayDB.InsertPaymentTransferLog("申请订单完成", 1, payment.PaymentSerial, payment.ProviderCode);
                    return revjsonObj["data"]["url"]["payUrl"].ToString();

                }
                else
                {
                    PayDB.InsertPaymentTransferLog("供应商回传有误:" + jsonStr, 1, payment.PaymentSerial, payment.ProviderCode);
                    return "";
                }
            }
            else
            {
                PayDB.InsertPaymentTransferLog("供应商回传有误:回传为空值", 1, payment.PaymentSerial, payment.ProviderCode);
                return "";
            }
        }
        catch (Exception ex)
        {
            PayDB.InsertPaymentTransferLog("系统错误:" + ex.Message, 1, payment.PaymentSerial, payment.ProviderCode);
            return "";
            throw;
        }

    }

    GatewayCommon.ProviderRequestType GatewayCommon.ProviderGateway.GetRequestType()
    {
        return SettingData.RequestType;
    }

    public GatewayCommon.PaymentByProvider QueryPayment(GatewayCommon.Payment payment)
    {
        GatewayCommon.PaymentByProvider Ret = new GatewayCommon.PaymentByProvider();
        string sign;
        string signStr = "";
        Dictionary<string, object> signDic = new Dictionary<string, object>();
        // 生成一个随机的 GUID
        Guid guid = Guid.NewGuid();
        // 将 GUID 转换成字符串
        string randomString = guid.ToString("N");
        // 获取当前时间的 DateTimeOffset 对象
        DateTimeOffset currentTime = DateTimeOffset.Now;
        // 将当前时间转换为时间戳（毫秒级）
        long timestamp = currentTime.ToUnixTimeMilliseconds();


        signDic.Add("mchKey", SettingData.MerchantCode);//
        signDic.Add("mchOrderNo", payment.PaymentSerial);//
        signDic.Add("nonce", randomString);//
        signDic.Add("timestamp", timestamp);//


        signDic = CodingControl.AsciiDictionary2(signDic);

        foreach (KeyValuePair<string, object> item in signDic)
        {
            signStr += item.Key + "=" + item.Value + "&";
        }
        signStr = signStr.Substring(0, signStr.Length - 1);
        signStr = signStr + SettingData.MerchantKey;

        sign = CodingControl.GetMD5(signStr, false);

        signDic.Add("sign", sign);

        var jsonStr = GatewayCommon.RequestJsonAPI(SettingData.QueryOrderUrl, JsonConvert.SerializeObject(signDic), payment.PaymentSerial, payment.ProviderCode);

        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["code"].ToString()== "200")
                {
                    if (revjsonObj["data"]["payStatus"].ToString().ToUpper() == "SUCCESS")
                    {
                        PayDB.InsertPaymentTransferLog("反查订单成功:订单状态为成功", 1, payment.PaymentSerial, payment.ProviderCode);
                        Ret.IsPaymentSuccess = true;
                    }
                    else
                    {
                        PayDB.InsertPaymentTransferLog("反查订单成功:订单状态为处理中", 1, payment.PaymentSerial, payment.ProviderCode);
                        Ret.IsPaymentSuccess = false;
                    }

                    return Ret;

                }
                else
                {
                    PayDB.InsertPaymentTransferLog("反查订单回传有误:" + jsonStr, 1, payment.PaymentSerial, payment.ProviderCode);
                    Ret.IsPaymentSuccess = false;
                    return Ret;
                }
            }
            else
            {
                PayDB.InsertPaymentTransferLog("反查订单回传有误:回传为空值", 1, payment.PaymentSerial, payment.ProviderCode);
                Ret.IsPaymentSuccess = false;
                return Ret;
            }
        }
        catch (Exception ex)
        {
            PayDB.InsertPaymentTransferLog("系统错误:" + ex.Message, 1, payment.PaymentSerial, payment.ProviderCode);
            Ret.IsPaymentSuccess = false;
            return Ret;
            throw;
        }
    }

    public GatewayCommon.BalanceByProvider QueryPoint(string Currency)
    {
        GatewayCommon.BalanceByProvider Ret = null;
        return null;
    }

    GatewayCommon.WithdrawalByProvider GatewayCommon.ProviderGatewayByWithdraw.QueryWithdrawal(GatewayCommon.Withdrawal withdrawal)
    {
        GatewayCommon.WithdrawalByProvider Ret = new GatewayCommon.WithdrawalByProvider();
        return Ret;

    }

    GatewayCommon.ReturnWithdrawByProvider GatewayCommon.ProviderGatewayByWithdraw.SendWithdrawal(GatewayCommon.Withdrawal withdrawal)
    {
        GatewayCommon.ReturnWithdrawByProvider retValue = new GatewayCommon.ReturnWithdrawByProvider() { ReturnResult = "", UpOrderID = "" };
        return retValue;
    }

    private GatewayCommon.ProviderSetting SettingData;




}