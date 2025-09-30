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
public class Provider_SGPay : GatewayCommon.ProviderGateway, GatewayCommon.ProviderGatewayByWithdraw
{

    public Provider_SGPay()
    {
        //初始化設定檔資料
        SettingData = GatewayCommon.GetProverderSettingData("SGPay");
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
        Dictionary<string, string> signDic = new Dictionary<string, string>();
        Dictionary<string, string> tokenDic = new Dictionary<string, string>();
        string token = "";

        tokenDic.Add("email", SettingData.OtherDatas[0]);
        tokenDic.Add("password", SettingData.OtherDatas[1]);

        var getToken = GatewayCommon.RequestJsonAPI(SettingData.ProviderUrl+ "/api/login", JsonConvert.SerializeObject(tokenDic), payment.PaymentSerial, payment.ProviderCode);

        try
        {
            if (!string.IsNullOrEmpty(getToken))
            {
                JObject revjsonObj = JObject.Parse(getToken);
                token= revjsonObj["access_token"].ToString();
            }
            else
            {
                PayDB.InsertPaymentTransferLog("get token error", 1, payment.PaymentSerial, payment.ProviderCode);
                return "";
            }
        }
        catch (Exception ex)
        {
            PayDB.InsertPaymentTransferLog("系统错误:" + ex.Message, 1, payment.PaymentSerial, payment.ProviderCode);
            return "";
            throw;
        }

        string hashCode= CodingControl.CalculateMD5("fiat_payment"+SettingData.MerchantKey);
        Guid newGuid = Guid.NewGuid();
        string customer_uid = newGuid.ToString("N");

        signDic.Add("command", "fiat_payment");
        signDic.Add("hashCode", hashCode);
        signDic.Add("callback_url", SettingData.NotifyAsyncUrl);//
        signDic.Add("redirect_url", SettingData.CallBackUrl);//
        signDic.Add("currency", "CNY");//
        signDic.Add("method", tradeTypeValue);
        signDic.Add("customer_uid", customer_uid);
        signDic.Add("txid", payment.PaymentSerial); 
        signDic.Add("depositor_name", "数字钱包");//
        signDic.Add("amount", payment.OrderAmount.ToString("#.##"));
        signDic.Add("merchant_number", SettingData.MerchantCode);//
 

        PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 1, payment.PaymentSerial, payment.ProviderCode);
        var jsonStr = GatewayCommon.RequestJsonAPIByAuthorization(SettingData.ProviderUrl+ "/api/callBack", JsonConvert.SerializeObject(signDic), payment.PaymentSerial, payment.ProviderCode, token);

        //This line executes whether or not the exception occurs.
        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                PayDB.InsertPaymentTransferLog("申请订单完成", 1, payment.PaymentSerial, payment.ProviderCode);
                return revjsonObj["pay_url"].ToString();
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
        string tradeTypeValue = SettingData.ServiceSettings.Find(x => x.ServiceType == payment.ServiceType).TradeType;
        Dictionary<string, string> signDic = new Dictionary<string, string>();
        Dictionary<string, string> tokenDic = new Dictionary<string, string>();
        string token = "";

        tokenDic.Add("email", SettingData.OtherDatas[0]);
        tokenDic.Add("password", SettingData.OtherDatas[1]);

        var getToken = GatewayCommon.RequestJsonAPI(SettingData.ProviderUrl + "/api/login", JsonConvert.SerializeObject(tokenDic), payment.PaymentSerial, payment.ProviderCode);

        try
        {
            if (!string.IsNullOrEmpty(getToken))
            {
                JObject revjsonObj = JObject.Parse(getToken);
                token = revjsonObj["access_token"].ToString();
            }
            else
            {
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

        string hashCode = CodingControl.CalculateMD5("fiat_payment_status" + SettingData.MerchantKey);
        Guid newGuid = Guid.NewGuid();
        string customer_uid = newGuid.ToString("N");

        signDic.Add("command", "fiat_payment_status");
        signDic.Add("hashCode", hashCode);
        signDic.Add("txid", payment.PaymentSerial);

        PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 1, payment.PaymentSerial, payment.ProviderCode);
        var jsonStr = GatewayCommon.RequestJsonAPIByAuthorization(SettingData.ProviderUrl + "/api/callBack", JsonConvert.SerializeObject(signDic), payment.PaymentSerial, payment.ProviderCode, token);

        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj["status"].ToString().ToUpper() == "COMPLETED")
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