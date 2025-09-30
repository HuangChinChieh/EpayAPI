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
public class Provider_JiuYuanPay : GatewayCommon.ProviderGateway, GatewayCommon.ProviderGatewayByWithdraw
{

    public Provider_JiuYuanPay()
    {
        //初始化設定檔資料
        SettingData = GatewayCommon.GetProverderSettingData("JiuYuanPay");
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

        signDic.Add("fxid", SettingData.MerchantCode);
        signDic.Add("fxddh", payment.PaymentSerial);
        signDic.Add("fxdesc", "fxdesc");
        signDic.Add("fxfee", payment.OrderAmount.ToString("#.##"));
        signDic.Add("fxnotifyurl", SettingData.NotifyAsyncUrl);
        signDic.Add("fxbackurl", SettingData.CallBackUrl);
        signDic.Add("fxpay", tradeTypeValue);
        signDic.Add("fxusername", payment.UserName);
        signDic.Add("fxip", payment.ClientIP);
        signDic.Add("fxuserid", payment.PaymentSerial);

        signStr = "fxid=" + SettingData.MerchantCode +
                 "&fxddh=" + payment.PaymentSerial +
                 "&fxfee=" + payment.OrderAmount.ToString("#.##") +
                 "&fxnotifyurl=" + SettingData.NotifyAsyncUrl +
                 "&" + SettingData.MerchantKey;


        sign = CodingControl.GetMD5(signStr, false);

        signDic.Add("fxsign", sign);
     
        PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(signDic), 1, payment.PaymentSerial, payment.ProviderCode);
        var jsonStr = GatewayCommon.RequestFormDataConentTypeAPI(SettingData.ProviderUrl, signDic, payment.PaymentSerial, payment.ProviderCode);

        //This line executes whether or not the exception occurs.
        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["status"].ToString() == "1")
                {
                    PayDB.InsertPaymentTransferLog("申请订单完成", 1, payment.PaymentSerial, payment.ProviderCode);
                    return revjsonObj["payurl"].ToString();

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
        Dictionary<string, string> signDic = new Dictionary<string, string>();
        int fxtype = 1;
        signDic.Add("fxid", SettingData.MerchantCode);//
        signDic.Add("fxtype", fxtype.ToString());//
        signDic.Add("fxorder", payment.PaymentSerial);//

        signStr = "fxid=" + SettingData.MerchantCode +
                  "&fxorder=" + payment.PaymentSerial +
                  "&fxtype=" + fxtype +
                  "&" + SettingData.MerchantKey;


        sign = CodingControl.GetMD5(signStr, false);
        signDic.Add("sign", sign);

        var jsonStr = GatewayCommon.RequestFormDataConentTypeAPI(SettingData.QueryOrderUrl, signDic, payment.PaymentSerial, payment.ProviderCode);

        try
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["result"].ToString().ToUpper()== "SUCCESS")
                {
                    if (revjsonObj["status"].ToString() == "1")
                    {
                        PayDB.InsertPaymentTransferLog("反查订单成功:订单状态为成功", 1, payment.PaymentSerial, payment.ProviderCode);
                        Ret.IsPaymentSuccess = true;
                    }
                    else
                    {
                        PayDB.InsertPaymentTransferLog("反查订单成功:订单状态为处理中:"+ jsonStr, 1, payment.PaymentSerial, payment.ProviderCode);
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
        GatewayCommon.WithdrawalByProvider retValue = new GatewayCommon.WithdrawalByProvider() { IsQuerySuccess = false, WithdrawalStatus = 2 };
        string sign;
        string signStr = "";
        Dictionary<string, string> sendDic = new Dictionary<string, string>();
        Dictionary<string, object> fxbody = new Dictionary<string, object>();
        try
        {
            fxbody.Add("fxddh", withdrawal.WithdrawSerial);

            sendDic.Add("fxid", SettingData.MerchantCode);//
            sendDic.Add("fxaction", "dfpayquery");//
            sendDic.Add("fxbody", JsonConvert.SerializeObject(fxbody));//

            signStr = "fxid=" + SettingData.MerchantCode +
                      "&fxaction=" + "dfpayquery"+
                      "&fxbody=" + JsonConvert.SerializeObject(fxbody) +
                      "&key=" + SettingData.MerchantKey;


            sign = CodingControl.GetMD5(signStr, false);
            sendDic.Add("fxsign", sign);

            var jsonStr = GatewayCommon.RequestFormDataConentTypeAPI(SettingData.QueryWithdrawUrl, sendDic, withdrawal.WithdrawSerial, withdrawal.ProviderCode);

            PayDB.InsertPaymentTransferLog("查詢代付訂單:" + JsonConvert.SerializeObject(sendDic), 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);
            PayDB.InsertPaymentTransferLog("signStr:" + signStr, 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);

            if (!string.IsNullOrEmpty(jsonStr))
            {

                JObject revjsonObj = JObject.Parse(jsonStr);
                PayDB.InsertPaymentTransferLog("查詢代付訂單結果:" + jsonStr, 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);
                //return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/Result.cshtml?ResultCode=" + jsonStr;

                if (revjsonObj != null && revjsonObj["fxstatus"].ToString() == "1")
                {
                    if (revjsonObj["fxbody"]["fxstatus"].ToString() == "1")
                    {
                        //已完成
                        retValue.WithdrawalStatus = 0;
                        retValue.Amount = 0;
                    }
                    if (revjsonObj["fxbody"]["fxstatus"].ToString() == "3")
                    {
                        //失敗
                        retValue.WithdrawalStatus = 1;
                        retValue.Amount = 0;
                    }
                    else
                    {
                        //處理中
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
                PayDB.InsertPaymentTransferLog("查詢代付訂單失敗，訂單資訊:"+ jsonStr, 5, withdrawal.WithdrawSerial, SettingData.ProviderCode);
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
        Dictionary<string, string> sendDic = new Dictionary<string, string>();
        Dictionary<string, object> fxbody = new Dictionary<string, object>();

        string sign;
        string signStr = "";
        string ProviderBankCode;

        try
        {

            var ProviderBankCodes = SettingData.BankCodeSettings.Where(w => w.BankName == withdrawal.BankName);

            if (ProviderBankCodes.Count() == 0)
            {
                retValue.ReturnResult = "不支援此银行";
                PayDB.InsertPaymentTransferLog("不支援此银行,银行名称:" + withdrawal.BankName, 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                retValue.SendStatus = 0;
                return retValue;
            }


            ProviderBankCode = ProviderBankCodes.First().BankCode;

            fxbody.Add("fxddh", withdrawal.WithdrawSerial);//
            fxbody.Add("fxfee", (int)Math.Floor(withdrawal.Amount));
            fxbody.Add("fxbankaccount", withdrawal.BankCard);//
            fxbody.Add("fxname", withdrawal.BankCardName);//
            fxbody.Add("fxbankname", ProviderBankCodes.First().BankName);//
            fxbody.Add("fxbankcode", ProviderBankCode);//
            fxbody.Add("fxzhihang", withdrawal.BankBranchName);//
            
            sendDic.Add("fxid", SettingData.MerchantCode);//
            sendDic.Add("fxaction", "dfpay");//
            sendDic.Add("fxnotifyurl", SettingData.WithdrawNotifyAsyncUrl);//
            sendDic.Add("fxpay", "dfjpy");//
            sendDic.Add("fxbody", JsonConvert.SerializeObject(fxbody));//


            signStr = "fxid=" + SettingData.MerchantCode +
               "&fxaction=" + "dfpay" +
               "&fxbody=" + JsonConvert.SerializeObject(fxbody) +
               "&key=" + SettingData.MerchantKey;


            sign = CodingControl.GetMD5(signStr, false);

            sendDic.Add("fxsign", sign);

            PayDB.InsertPaymentTransferLog("通知供应商,传出资料:" + JsonConvert.SerializeObject(sendDic), 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);

            var jsonStr = GatewayCommon.RequestFormDataConentTypeAPI(SettingData.WithdrawUrl, sendDic, withdrawal.WithdrawSerial, withdrawal.ProviderCode);

            if (!string.IsNullOrEmpty(jsonStr))
            {

                JObject revjsonObj = JObject.Parse(jsonStr);

                if (revjsonObj != null && revjsonObj["fxstatus"].ToString().ToUpper() == "1")
                {
                    retValue.SendStatus = 1;
                    retValue.UpOrderID = "";
                    retValue.WithdrawSerial = withdrawal.WithdrawSerial;
                    retValue.ReturnResult = jsonStr;
                    PayDB.InsertPaymentTransferLog("申请订单完成", 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                }
                else
                {
                    if (revjsonObj.ContainsKey("fxmsg"))
                    {
                        PayDB.InsertPaymentTransferLog("供应商回传有误:" + revjsonObj.ContainsKey("fxmsg").ToString(), 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                        retValue.ReturnResult = jsonStr;
                        retValue.SendStatus = 0;
                    }
                    else {
                        PayDB.InsertPaymentTransferLog("供应商回传有误:" + jsonStr, 5, withdrawal.WithdrawSerial, withdrawal.ProviderCode);
                        retValue.ReturnResult = jsonStr;
                        retValue.SendStatus = 0;
                    }
                 
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