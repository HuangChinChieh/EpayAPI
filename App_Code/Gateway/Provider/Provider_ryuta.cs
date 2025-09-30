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
public class Provider_ryuta : GatewayCommon.ProviderGateway, GatewayCommon.ProviderGatewayByWithdraw
{

    public Provider_ryuta()
    {
        //初始化設定檔資料
        SettingData = GatewayCommon.GetProverderSettingData("ryuta");
    }

    Dictionary<string, string> GatewayCommon.ProviderGateway.GetSubmitData(GatewayCommon.Payment payment)
    {
        Dictionary<string, string> dataDic = new Dictionary<string, string>();
        return dataDic;
    }

    string GatewayCommon.ProviderGateway.GetCompleteUrl(GatewayCommon.Payment payment)
    {
        string sign;
        string tradeTypeValue = SettingData.ServiceSettings.Find(x => x.ServiceType == payment.ServiceType).TradeType;
        Dictionary<string, string> signDic = new Dictionary<string, string>();

        signDic.Add("transaction_id", payment.PaymentSerial);//
        signDic.Add("amount", payment.OrderAmount.ToString("#.##"));
        signDic.Add("payment_method", tradeTypeValue);
        signDic.Add("notify_url", SettingData.NotifyAsyncUrl);//

        string token = SettingData.ProviderPublicKey;
        string secret = SettingData.MerchantKey;
        string data = token + "amount="+ payment.OrderAmount.ToString("#.##") + "&payment_method="+ tradeTypeValue + "&transaction_id="+ payment.PaymentSerial;

        sign= CodingControl.GetHmacSHA256(secret, data).Replace('+', '-').Replace('/', '_');

        signDic.Add("hash", sign);
     
        PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 1, payment.PaymentSerial, payment.ProviderCode);
        var jsonStr = GatewayCommon.RequestFormDataConentTypeAPIByAuthorization(SettingData.ProviderUrl, signDic, payment.PaymentSerial, payment.ProviderCode, SettingData.ProviderPublicKey);

        //This line executes whether or not the exception occurs.
        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                PayDB.InsertPaymentTransferLog("申请订单完成", 1, payment.PaymentSerial, payment.ProviderCode);
                PayDB.UpdatePaymentProviderOrderID(payment.PaymentID, revjsonObj["data"]["transaction_reference"].ToString());
                return revjsonObj["data"]["redirect_to"].ToString();

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
        string token = SettingData.ProviderPublicKey;
        var jsonStr = GatewayCommon.RequestGetAPIByAuthorization(SettingData.QueryOrderUrl+ payment.ProviderOrderID, "", payment.PaymentSerial, payment.ProviderCode, token);

        try
        {
       
            JObject revjsonObj = JObject.Parse(jsonStr);

            if (revjsonObj["data"]["status"].ToString().ToLower() == "completed")
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
        GatewayCommon.WithdrawalByProvider retValue = new GatewayCommon.WithdrawalByProvider() { IsQuerySuccess = false };
        string sign;
        string signStr = "";
        string GUID = Guid.NewGuid().ToString("N");
        Dictionary<string, object> signDic = new Dictionary<string, object>();
        Dictionary<string, string> sendDic = new Dictionary<string, string>();

        try
        {

            string token = SettingData.ProviderPublicKey;
            var jsonStr = GatewayCommon.RequestGetAPIByAuthorization(SettingData.QueryWithdrawUrl + withdrawal.UpOrderID, "", withdrawal.WithdrawSerial, withdrawal.ProviderCode, token);

            if (!string.IsNullOrEmpty(jsonStr))
            {

                JObject revjsonObj = JObject.Parse(jsonStr);
                PayDB.InsertPaymentTransferLog("查詢代付訂單結果:" + jsonStr, 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);

                if (revjsonObj != null)
                {
                    if (revjsonObj["data"]["status"].ToString().ToLower() == "pending"|| revjsonObj["data"]["status"].ToString().ToLower() == "new")
                    {
                        //處理中

                        retValue.WithdrawalStatus = 2;
                        retValue.Amount = 0;
                    }
                    else if (revjsonObj["data"]["status"].ToString().ToLower() == "completed")
                    {
                        //已完成
                        retValue.WithdrawalStatus = 0;
                        retValue.Amount = 0;
                    }
                    else if (revjsonObj["data"]["status"].ToString().ToLower() == "failed" || revjsonObj["data"]["status"].ToString().ToLower() == "refund" || revjsonObj["data"]["status"].ToString().ToLower() == "rejected")
                    {
                        //失敗
                        retValue.WithdrawalStatus = 1;
                        retValue.Amount = 0;
                    }
                    else
                    {
                        retValue.WithdrawalStatus = 2;
                        retValue.Amount = 0;
                    }

                    retValue.IsQuerySuccess = true;
                    retValue.UpOrderID = "";
                    retValue.ProviderCode = withdrawal.ProviderCode;
                    retValue.ProviderReturn = jsonStr;
                }
            }
            else
            {
                PayDB.InsertPaymentTransferLog(" get json retrun error;", 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);
                retValue.WithdrawalStatus = 2;
                retValue.IsQuerySuccess = false;
                retValue.UpOrderID = "";
                retValue.ProviderCode = withdrawal.ProviderCode;
                retValue.ProviderReturn = "";
                retValue.Amount = 0;
            }
        }
        catch (Exception ex)
        {
            PayDB.InsertPaymentTransferLog("Exception:" + ex.Message, 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);
            retValue.WithdrawalStatus = 2;
            retValue.IsQuerySuccess = false;
            retValue.UpOrderID = "";
            retValue.ProviderCode = withdrawal.ProviderCode;
            retValue.ProviderReturn = "";
            retValue.Amount = 0;
            throw;
        }

        return retValue;
    }

    GatewayCommon.ReturnWithdrawByProvider GatewayCommon.ProviderGatewayByWithdraw.SendWithdrawal(GatewayCommon.Withdrawal withdrawal)
    {
        GatewayCommon.ReturnWithdrawByProvider retValue = new GatewayCommon.ReturnWithdrawByProvider() { ReturnResult = "", UpOrderID = "" };
      
        string sign;
        Dictionary<string, string> signDic = new Dictionary<string, string>();
        string ProviderBankCode = "";
        try
        {
            var array = GatewayCommon.GetWithdrawBankSettingData(withdrawal.CurrencyType);
            JObject jo = array.Children<JObject>()
        .FirstOrDefault(o => o["BankName"] != null && o["BankName"].ToString() == withdrawal.BankName);

            if (jo == null)
            {
                retValue.ReturnResult = "不支援此银行";
         
                PayDB.InsertPaymentTransferLog("不支援此银行,银行名称:" + withdrawal.BankName, 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                retValue.SendStatus = 0;
                return retValue;
            }

            ProviderBankCode = jo["BankCode"].ToString();

            signDic.Add("transaction_id", withdrawal.WithdrawSerial);//
            signDic.Add("bank_code", ProviderBankCode);//
            signDic.Add("account_number", withdrawal.BankCard);//
            signDic.Add("name", withdrawal.BankCardName);//
            signDic.Add("amount", withdrawal.Amount.ToString("#.##"));//
            signDic.Add("notify_url", SettingData.WithdrawNotifyAsyncUrl);//
            

            string token = SettingData.ProviderPublicKey;
            string secret = SettingData.MerchantKey;
            string data = token + "account_number=" + withdrawal.BankCard + "&amount=" + withdrawal.Amount.ToString("#.##") + "&bank_code=" + ProviderBankCode + "&transaction_id=" + withdrawal.WithdrawSerial;
            sign = CodingControl.GetHmacSHA256(secret, data).Replace('+', '-').Replace('/', '_');

            signDic.Add("hash", sign);

            PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
            var jsonStr = GatewayCommon.RequestFormDataConentTypeAPIByAuthorization(SettingData.WithdrawUrl, signDic, withdrawal.WithdrawSerial, withdrawal.ProviderCode, SettingData.ProviderPublicKey);
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["data"]["status"].ToString().ToLower() == "new")
                {
                    retValue.SendStatus = 1;
                    retValue.UpOrderID = "";
                    retValue.WithdrawSerial = withdrawal.WithdrawSerial;
                    retValue.ReturnResult = jsonStr;
                    PayDB.UpdateWithdrawProviderOrderNumberByID(withdrawal.WithdrawID, revjsonObj["data"]["transaction_reference"].ToString());
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