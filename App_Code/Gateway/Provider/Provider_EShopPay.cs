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
public class Provider_EShopPay : GatewayCommon.ProviderGateway, GatewayCommon.ProviderGatewayByWithdraw
{

    public Provider_EShopPay()
    {
        //初始化設定檔資料
        SettingData = GatewayCommon.GetProverderSettingData("EShopPay");
    }

    Dictionary<string, string> GatewayCommon.ProviderGateway.GetSubmitData(GatewayCommon.Payment payment)
    {
        Dictionary<string, string> dataDic = new Dictionary<string, string>();
        return dataDic;
    }

    string GatewayCommon.ProviderGateway.GetCompleteUrl(GatewayCommon.Payment payment)
    {
        string sign;
        Dictionary<string, object> signDic = new Dictionary<string, object>();

        string OrderDate= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        signDic.Add("StoreCode", SettingData.MerchantCode);//
        signDic.Add("EMail", "a776548@gmail.com");//
        signDic.Add("Realname", payment.UserName);//
        signDic.Add("ContactNumber", "");//
        signDic.Add("ChannelCode", "Stripe");//
        signDic.Add("Currency", "JPY");//
        signDic.Add("OrderNumber", payment.PaymentSerial);//
        signDic.Add("Amount", payment.OrderAmount.ToString("#.##"));
        signDic.Add("OrderDate", OrderDate);//

        sign = GetSign(SettingData.MerchantCode,SettingData.MerchantKey, "", payment.UserName, payment.OrderAmount, payment.PaymentSerial, "JPY", OrderDate);

        signDic.Add("Sign", sign);
     
        PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 1, payment.PaymentSerial, payment.ProviderCode);
        var jsonStr = GatewayCommon.RequestJsonAPI(SettingData.ProviderUrl, JsonConvert.SerializeObject(signDic), payment.PaymentSerial, payment.ProviderCode);

        //This line executes whether or not the exception occurs.
        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["ResultState"].ToString() == "0")
                {
                    PayDB.InsertPaymentTransferLog("申请订单完成", 1, payment.PaymentSerial, payment.ProviderCode);
                    return revjsonObj["Message"].ToString();

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

    public string GetSign(string StoreCode, string ApiKey, string EMail, string Realname, decimal Amount, string OrderNumber, string Currency, string OrderDate)
    {
        string sign;
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
        Dictionary<string, string> Dic = new Dictionary<string, string>
        {
            { "StoreCode", StoreCode },
            { "Realname", Realname },
            { "Amount", Amount.ToString("#.##") },
            { "OrderNumber", OrderNumber },
            { "Currency", Currency },
            { "OrderDate", OrderDate }
        };

        foreach (var property in Dic.OrderBy(x => x.Key))
        {
            stringBuilder.Append(property.Key + "=" + property.Value + "&");
        }

        stringBuilder.Append("Key" + "=" + ApiKey);

        sign = CodingControl.GetMD5(stringBuilder.ToString(), false).ToLower();

        return sign;
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

        signDic.Add("merchantId", SettingData.MerchantCode);//
        signDic.Add("orderId", payment.PaymentSerial);//

        signDic = CodingControl.AsciiDictionary2(signDic);

        foreach (KeyValuePair<string, object> item in signDic)
        {
            signStr += item.Key + "=" + item.Value + "&";
        }

        signStr = signStr + "key=" + SettingData.MerchantKey;

        sign = CodingControl.GetMD5(signStr, false).ToLower();

        signDic.Add("Sign", sign);

        signDic = CodingControl.AsciiDictionary2(signDic);

        var jsonStr = GatewayCommon.RequestJsonAPI(SettingData.QueryOrderUrl, JsonConvert.SerializeObject(signDic), payment.PaymentSerial, payment.ProviderCode);

        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["code"].ToString()== "200")
                {
                    if (revjsonObj["data"]["status"].ToString().ToUpper() == "PAID")
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

        string sign;
        Dictionary<string, string> signDic = new Dictionary<string, string>();
    
        try
        {

            string OrderDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            signDic.Add("StoreCode", SettingData.MerchantCode);//
            signDic.Add("EMail", "a776548@gmail.com");//
            signDic.Add("Realname", withdrawal.BankCardName);//
            signDic.Add("Currency", "JPY");//
            signDic.Add("OrderNumber", withdrawal.WithdrawSerial);//
            signDic.Add("Amount", withdrawal.Amount.ToString("#.##"));
            signDic.Add("OrderDate", OrderDate);//
            signDic.Add("BankBranch", withdrawal.BankBranchName);//
            signDic.Add("BankName", withdrawal.BankName);//
            signDic.Add("BankNumber", withdrawal.BankCard);//
            signDic.Add("BankProvince", "BankProvince");//
            signDic.Add("BankCity", "BankCity");//
            signDic.Add("ContactNumber", "09881234596");//
            signDic.Add("ContactAddress", "ContactAddress");//
            sign = GetSign(SettingData.MerchantCode, SettingData.MerchantKey, "", withdrawal.BankCardName, withdrawal.Amount, withdrawal.WithdrawSerial, "JPY", OrderDate);
            signDic.Add("Sign", sign);
           
            PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
            var jsonStr = GatewayCommon.RequestJsonAPI(SettingData.WithdrawUrl, JsonConvert.SerializeObject(signDic), withdrawal.WithdrawSerial, withdrawal.ProviderCode);
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["ResultState"].ToString() == "0")
                {
                    retValue.SendStatus = 1;
                    retValue.UpOrderID = "";
                    retValue.WithdrawSerial = withdrawal.WithdrawSerial;
                    retValue.ReturnResult = jsonStr;
                   
                    PayDB.InsertPaymentTransferLog("申请订单完成", 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                }
                else
                {
                    PayDB.InsertPaymentTransferLog("供应商回传有误:" + jsonStr, 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                    retValue.ReturnResult = jsonStr;
                    retValue.SendStatus = 0;
                }

            }
            else
            {
                PayDB.InsertPaymentTransferLog("供应商回传有误:回传为空值", 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                retValue.ReturnResult = jsonStr;
                retValue.SendStatus = 0;
            }
        }
        catch (Exception ex)
        {
            PayDB.InsertPaymentTransferLog("系统错误:" + ex.Message, 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);
            retValue.ReturnResult = ex.Message;
            retValue.SendStatus = 0;
            throw;
        }
        return retValue;
    }

    private GatewayCommon.ProviderSetting SettingData;




}