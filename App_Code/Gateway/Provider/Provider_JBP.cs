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
public class Provider_JBP : GatewayCommon.ProviderGateway, GatewayCommon.ProviderGatewayByWithdraw
{

    public Provider_JBP()
    {
        //初始化設定檔資料
        SettingData = GatewayCommon.GetProverderSettingData("JBP");
    }

    Dictionary<string, string> GatewayCommon.ProviderGateway.GetSubmitData(GatewayCommon.Payment payment)
    {
        Dictionary<string, string> dataDic = new Dictionary<string, string>();
        return dataDic;
    }

    string GatewayCommon.ProviderGateway.GetCompleteUrl(GatewayCommon.Payment payment)
    {
       return "";

    }

    GatewayCommon.ProviderRequestType GatewayCommon.ProviderGateway.GetRequestType()
    {
        return SettingData.RequestType;
    }

    public GatewayCommon.BalanceByProvider QueryPoint(string Currency)
    {
        GatewayCommon.BalanceByProvider Ret = null;
        return null;
    }

    public GatewayCommon.PaymentByProvider QueryPayment(GatewayCommon.Payment payment)
    {
        GatewayCommon.PaymentByProvider Ret = new GatewayCommon.PaymentByProvider();
        string sign;
        string signStr = "";
        Dictionary<string, string> signDic = new Dictionary<string, string>();

        return Ret;
    }

    GatewayCommon.WithdrawalByProvider GatewayCommon.ProviderGatewayByWithdraw.QueryWithdrawal(GatewayCommon.Withdrawal withdrawal)
    {
        GatewayCommon.WithdrawalByProvider retValue = new GatewayCommon.WithdrawalByProvider() { IsQuerySuccess = false };
        string sign;
        Dictionary<string, object> signDic = new Dictionary<string, object>();
        Dictionary<string, string> sendDic = new Dictionary<string, string>();

        try
        {
            signDic.Add("company_id", SettingData.MerchantCode);//
            signDic.Add("bank_id", 1);//
            signDic.Add("amount", withdrawal.Amount.ToString("F2"));//
            signDic.Add("company_order_num", withdrawal.WithdrawSerial);//
            signDic.Add("company_user", "company_user");//
            signDic.Add("card_num", withdrawal.BankCard);//
            signDic.Add("card_name", withdrawal.BankCardName);//
            signDic.Add("memo", withdrawal.BankName);//
            signDic.Add("web_url", SettingData.WithdrawNotifyAsyncUrl);//
            signDic.Add("clientIp", CodingControl.GetUserIP());//
            signDic.Add("accountType", 1);//

            sign = CodingControl.CalculateMD5(CodingControl.CalculateMD5(SettingData.MerchantKey) + SettingData.MerchantCode + 1 + withdrawal.WithdrawSerial + withdrawal.Amount.ToString("F2"));

            signDic.Add("key", sign);

            PayDB.InsertPaymentTransferLog("查詢代付訂單:" + JsonConvert.SerializeObject(signDic), 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);

            var jsonStr = GatewayCommon.RequestJsonAPI(SettingData.QueryWithdrawUrl, JsonConvert.SerializeObject(signDic), withdrawal.WithdrawSerial, withdrawal.ProviderCode);

            if (!string.IsNullOrEmpty(jsonStr))
            {

                JObject revjsonObj = JObject.Parse(jsonStr);
                PayDB.InsertPaymentTransferLog("查詢代付訂單結果:" + jsonStr, 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);
                //return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/Result.cshtml?ResultCode=" + jsonStr;

                if (revjsonObj["status"].ToString() == "0")
                {
                    //處理中

                    retValue.WithdrawalStatus = 3;
                    retValue.Amount = 0;
                }
                else if (revjsonObj["status"].ToString() == "1")
                {
                    //已完成
                    retValue.WithdrawalStatus = 0;
                    retValue.Amount = 0;
                }
                else if (revjsonObj["status"].ToString() == "2")
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

        Dictionary<string, object> signDic = new Dictionary<string, object>();
        string sign;
        try
        {
        
            signDic.Add("company_id", SettingData.MerchantCode);//
            signDic.Add("bank_id", 1);//
            signDic.Add("amount", withdrawal.Amount.ToString("F2"));//
            signDic.Add("company_order_num", withdrawal.WithdrawSerial);//
            signDic.Add("company_user", "company_user");//
            signDic.Add("card_num", withdrawal.BankCard);//
            signDic.Add("card_name", withdrawal.BankCardName);//
            signDic.Add("memo", withdrawal.BankName);//
            signDic.Add("web_url", SettingData.WithdrawNotifyAsyncUrl);//
            signDic.Add("clientIp", CodingControl.GetUserIP());//

            signDic.Add("issue_bank_name", "issue_bank_name");//
            signDic.Add("issue_bank_address", "issue_bank_address");//
            signDic.Add("accountType", 1);//

            sign=CodingControl.CalculateMD5(CodingControl.CalculateMD5(SettingData.MerchantKey) + SettingData.MerchantCode + 1 + withdrawal.WithdrawSerial + withdrawal.Amount.ToString("F2") + withdrawal.BankCard + withdrawal.BankCardName + "company_user" + SettingData.WithdrawNotifyAsyncUrl + "issue_bank_name" + "issue_bank_address" + withdrawal.BankName);
      
            signDic.Add("key", sign);

            PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);

            var jsonStr = GatewayCommon.RequestJsonAPI(SettingData.WithdrawUrl, JsonConvert.SerializeObject(signDic), withdrawal.WithdrawSerial, withdrawal.ProviderCode);

            if (!string.IsNullOrEmpty(jsonStr))
            {

                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["status"].ToString().ToUpper() == "1")
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