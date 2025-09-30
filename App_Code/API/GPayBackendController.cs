using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Results;
using Ext.Net;
using Nethereum.RPC.Eth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GPayBackendController : ApiController {


    [HttpGet]
    [ActionName("GetUnSendMsgToBot")]
    public MsgList GetUnSendMsgToBot(int LastMsgID) {

        MsgList ReturnMsgList = new MsgList();
        //ReturnMsgList.List = new List<Msg>();
        ReturnMsgList.List = GatewayCommon.ToList<Msg>(PayDB.GetUnSendMsgToBot(LastMsgID)) as List<Msg>;

        return ReturnMsgList;
    }

    [HttpGet]
    [ActionName("SetMsgToBotSended")]
    public void SetMsgToBotSended(int LastMsgID) {
         
        PayDB.SetMsgToBotSended(LastMsgID);
    }

    [HttpGet]
    [ActionName("QueryProviderOrder")]
    public string QueryProviderOrder(string PaymentSerial)
    {

        var DT = PayDB.GetPaymentByPaymentID(PaymentSerial);
        GatewayCommon.Payment paymentModel;
        paymentModel = GatewayCommon.ToList<GatewayCommon.Payment>(DT).FirstOrDefault();
        GatewayCommon.PaymentByProvider providerRequestData = new GatewayCommon.PaymentByProvider();
        providerRequestData = GatewayCommon.QueryPaymentByProvider(paymentModel);
      
        return JsonConvert.SerializeObject(providerRequestData);
    }


    [HttpPost]
    [ActionName("GetProviderPointList")]
    public GetProviderPointListResult GetProviderPointList([FromBody] FromBodyQueryPointByPayment body)
    {

        #region SignCheck
        string strSign;
        string sign;
        GetProviderPointListResult Ret = new GetProviderPointListResult() { Providers = new List<FromBodyQueryProviderPoint>() };
        strSign = string.Format("GPayBackendKey={0}"
        , Pay.GPayBackendKey
        );
   
        sign = CodingControl.GetSHA256(strSign);

        if (sign.ToUpper() == body.Sign.ToUpper())
        {
            string jsonResult = "";
            var DT = PayDB.GetProviderPointList();

            if (DT != null && DT.Rows.Count > 0)
            {

                jsonResult = JsonConvert.SerializeObject(DT);
                Ret.Status = ResultStatus.OK;
                Ret.Providers = GatewayCommon.ToList<FromBodyQueryProviderPoint>(DT).ToList();

            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "沒有資料";
            }
        }
        else
        {
            Ret.Status = ResultStatus.SignErr;
            Ret.Message = "簽名有誤";
        }

        #endregion

        return Ret;
    }

    [HttpPost]
    [ActionName("QueryProviderPoint")]
    public QueryProviderPointResult QueryProviderPoint([FromBody] FromBodyQueryProviderPoint body)
    {
        #region SignCheck
        string strSign;
        string sign;
        QueryProviderPointResult Ret = new QueryProviderPointResult() { BalanceData = new GatewayCommon.BalanceByProvider() };
        strSign = string.Format("CurrencyType={0}&ProviderCode={1}&GPayBackendKey={2}"
        , body.CurrencyType
        , body.ProviderCode
        , Pay.GPayBackendKey
        );

        sign = CodingControl.GetSHA256(strSign);

        if (sign.ToUpper() == body.Sign.ToUpper())
        {
            GatewayCommon.BalanceByProvider providerRequestData = new GatewayCommon.BalanceByProvider();
            providerRequestData = GatewayCommon.QueryProviderBalance(body.ProviderCode, body.CurrencyType);

            if (providerRequestData != null)
            {

                Ret.Status = ResultStatus.OK;
                Ret.BalanceData = providerRequestData;

            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "沒有資料";
            }
        }
        else
        {
            Ret.Status = ResultStatus.SignErr;
            Ret.Message = "簽名有誤";
        }

        #endregion

        return Ret;
    }


    [HttpPost]
    [ActionName("QueryProviderBalance")]
    public ProviderBalanceResult QueryProviderBalance([FromBody] FromBodyBalance body) {
        ProviderBalanceResult Ret = new ProviderBalanceResult() { ArrayProviderBalance = new List<ProviderBalance>() };

        #region SignCheck
        string strSign;
        string sign;

        strSign = string.Format("CurrencyType={0}&GPayBackendKey={1}"
        , body.CurrencyType
        , Pay.GPayBackendKey
        );

        sign = CodingControl.GetSHA256(strSign);

        #endregion

        if (sign.ToUpper() == body.Sign.ToUpper()) {
            GatewayCommon.Provider provider;

            foreach (var item in body.ArrayProviderCode) {
                provider = GatewayCommon.ToList<GatewayCommon.Provider>(RedisCache.ProviderCode.GetProviderCode(item)).FirstOrDefault();

                //if (((GatewayCommon.ProviderAPIType)provider.ProviderAPIType & GatewayCommon.ProviderAPIType.QueryBalance) != GatewayCommon.ProviderAPIType.QueryBalance) {
                //    Ret.Status = ResultStatus.ERR;
                //    Ret.Message = "該廠商不支援此功能";
                //    return Ret;
                //}

                var apiReturn = GatewayCommon.QueryProviderBalance(item, body.CurrencyType);
                var providerBalance = new ProviderBalance();

                providerBalance.ProviderCode = item;

                if (apiReturn != null) {
                    providerBalance.IsProviderSupport = true;
                    providerBalance.AccountBalance = apiReturn.AccountBalance;
                    providerBalance.CashBalance = apiReturn.CashBalance;
                    providerBalance.CurrencyType = body.CurrencyType;
                    providerBalance.ProviderReturn = apiReturn.ProviderReturn;
                } else {
                    providerBalance.IsProviderSupport = false;
                }

                Ret.ArrayProviderBalance.Add(providerBalance);
            }
        } else {
            Ret.Status = ResultStatus.SignErr;
            Ret.Message = "簽名有誤";
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("GetTestSign")]
    public string GetTestSign([FromBody] FromBodyTest body) {
        string strSign;
        string sign;

        if (body.Type == 0) {
            strSign = string.Format("CompanyCode={0}&PaymentSerial={1}&GPayBackendKey={2}"
            , body.CompanyCode
            , body.PaymentSerial
            , Pay.GPayBackendKey
            );

            return sign = CodingControl.GetSHA256(strSign);
        } else if (body.Type == 1) {
            strSign = string.Format("CurrencyType={0}&GPayBackendKey={1}"
             , body.CurrencyType
             , Pay.GPayBackendKey
             );

            return sign = CodingControl.GetSHA256(strSign);
        } else if (body.Type == 2) {
            var companyKey = GatewayCommon.ToList<GatewayCommon.Company>(RedisCache.Company.GetCompanyByCode(body.CompanyCode)).FirstOrDefault().CompanyKey;
            return GatewayCommon.GetGPaySign(body.OrderID, decimal.Parse(body.OrderAmount), DateTime.Parse(body.OrderDate), body.ServiceType, body.CurrencyType, body.CompanyCode, companyKey);
        } else {
            return "error";
        }
    }
    
    [HttpPost]
    [ActionName("GetTestSign2")]
    public string GetTestSign2([FromBody] FromBodyTest body)
    {
        var companyKey = GatewayCommon.ToList<GatewayCommon.Company>(RedisCache.Company.GetCompanyByCode(body.CompanyCode)).FirstOrDefault().CompanyKey;
        return GatewayCommon.GetGPayWithdrawSign(body.OrderID, decimal.Parse(body.OrderAmount), DateTime.Parse(body.OrderDate), body.CurrencyType, body.CompanyCode, companyKey);
    }

    [HttpPost]
    [ActionName("ReSendPaymentByManualPayment")]
    public APIResult ReSendPaymentByManualPayment([FromBody] FromBodyReSendPayment body)
    {
        GatewayCommon.Payment paymentModel;
        GatewayCommon.GPayReturn gpayReturn = new GatewayCommon.GPayReturn() { SetByPaymentRetunData = new GatewayCommon.SetByPaymentRetunData() };
        APIResult Ret = new APIResult();
        bool companyRequestResult = false;

        #region SignCheck
        string strSign;
        string sign;

        strSign = string.Format("PaymentSerial={0}&GPayBackendKey={1}"
        , body.PaymentSerial
        , Pay.GPayBackendKey
        );

        sign = CodingControl.GetSHA256(strSign);

        #endregion

        #region 檢查Sign

        //簽名檢查
        if (sign != body.Sign)
        {
            Ret.Status = ResultStatus.SignErr;
            Ret.Message = "簽名有誤";

            return Ret;
        }

        paymentModel = PayDB.GetPaymentByPaymentID(body.PaymentSerial).ToList<GatewayCommon.Payment>().FirstOrDefault();

        #region 單號檢查

        if (!(paymentModel != null && paymentModel.PaymentID != 0))
        {
            Ret.Status = ResultStatus.Invalidate;
            Ret.Message = "查無此單";

            return Ret;
        }

        #endregion

        #region Status 是否已經進入可以使用API下發之狀態


        if (!(paymentModel.ProcessStatus == 2|| paymentModel.ProcessStatus == 4))
        {
            Ret.Status = ResultStatus.Invalidate;
            Ret.Message = "此流程無法補單";
            return Ret;
        }

        #endregion

        System.Data.DataTable CompanyDT = PayDB.GetCompanyByID(paymentModel.forCompanyID, true);
        if (!(CompanyDT != null && CompanyDT.Rows.Count > 0))
        {
            Ret.Status = ResultStatus.ERR;
            Ret.Message = "找不到此商户资讯";

            return Ret;
        }

        GatewayCommon.Company CompanyModel = GatewayCommon.ToList<GatewayCommon.Company>(CompanyDT).FirstOrDefault();

        System.Threading.Tasks.Task.Run(() =>
        {
            gpayReturn.SetByPayment(paymentModel, GatewayCommon.PaymentResultStatus.Successs);
    
            if (CompanyModel.IsProxyCallBack==0)
            {
                companyRequestResult = GatewayCommon.ReturnCompany4(30, gpayReturn, paymentModel.ProviderCode);
            }
            else
            {
                companyRequestResult = GatewayCommon.ReturnCompany(30, gpayReturn, paymentModel.ProviderCode);
            }

            if (companyRequestResult)
            {
                PayDB.UpdatePaymentComplete(paymentModel.PaymentSerial);
            }
        });

        #endregion


        return Ret;
    }

    [HttpPost]
    [ActionName("ReSendPayment")]
    public APIResult ReSendPayment([FromBody] FromBodyReSendPayment body)
    {
        GatewayCommon.Payment paymentModel;
        GatewayCommon.GPayReturn gpayReturn = new GatewayCommon.GPayReturn() { SetByPaymentRetunData = new GatewayCommon.SetByPaymentRetunData() };
        APIResult Ret = new APIResult();
        bool companyRequestResult = false;

        #region SignCheck
        string strSign;
        string sign;
  
        strSign = string.Format("PaymentSerial={0}&GPayBackendKey={1}"
        , body.PaymentSerial
        , Pay.GPayBackendKey
        );

        sign = CodingControl.GetSHA256(strSign);

        #endregion

        #region 檢查Sign

        //簽名檢查
        if (sign != body.Sign)
        {
            Ret.Status = ResultStatus.SignErr;
            Ret.Message = "簽名有誤";

            return Ret;
        }

        paymentModel = PayDB.GetPaymentByPaymentID(body.PaymentSerial).ToList<GatewayCommon.Payment>().FirstOrDefault();

        #region 單號檢查

        if (!(paymentModel != null && paymentModel.PaymentID != 0))
        {
            Ret.Status = ResultStatus.Invalidate;
            Ret.Message = "查無此單";

            return Ret;
        }

        #endregion

        #region Status 是否已經進入可以使用API下發之狀態


        if (paymentModel.ProcessStatus != 2)
        {


            Ret.Status = ResultStatus.Invalidate;
            Ret.Message = "此流程無法補單";
            return Ret;

        }

        #endregion

        System.Data.DataTable CompanyDT = PayDB.GetCompanyByID(paymentModel.forCompanyID, true);
        if (!(CompanyDT != null && CompanyDT.Rows.Count > 0))
        {
            Ret.Status = ResultStatus.ERR;
            Ret.Message = "找不到此商户资讯";

            return Ret;
        }
        
        GatewayCommon.Company CompanyModel = GatewayCommon.ToList<GatewayCommon.Company>(CompanyDT).FirstOrDefault();

        if (paymentModel.ProcessStatus == 2 || paymentModel.ProcessStatus == 4)
        {
            gpayReturn.SetByPayment(paymentModel, GatewayCommon.PaymentResultStatus.Successs);
        }
        else if (paymentModel.ProcessStatus == 3)
        {
            gpayReturn.SetByPayment(paymentModel, GatewayCommon.PaymentResultStatus.Failure);
        }
        else {
            gpayReturn.SetByPayment(paymentModel, GatewayCommon.PaymentResultStatus.PaymentProgress);
        }
       
      
        if (CompanyModel.IsProxyCallBack==0)
        {
            companyRequestResult = GatewayCommon.ReturnCompany3(gpayReturn, paymentModel.ProviderCode);
        }
        else
        {
            companyRequestResult = GatewayCommon.ReturnCompany2(gpayReturn, paymentModel.ProviderCode);
        }

        #endregion
    
        if (companyRequestResult) {
            PayDB.UpdatePaymentComplete(paymentModel.PaymentSerial);
       
        }
   
        return Ret;
    }

    [HttpPost]
    [ActionName("ReSendWithdraw")]
    public APIResult ReSendWithdraw([FromBody] FromBodyReSendWithdraw body)
    {
        GatewayCommon.Withdrawal withdrawalModel;
        GatewayCommon.GPayReturnByWithdraw gpayReturn = new GatewayCommon.GPayReturnByWithdraw() { SetByWithdrawRetunData = new GatewayCommon.SetByWithdrawRetunData() };
        APIResult Ret = new APIResult();
     
        try
        {
            #region SignCheck
            string strSign;
            string sign;
            GatewayCommon.WithdrawResultStatus WithdrawResultStatus;
            #endregion

            #region 檢查Sign

            strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
                                    , body.WithdrawSerial
                                    , Pay.GPayBackendKey
                                    );

            sign = CodingControl.GetSHA256(strSign);

            if (sign != body.Sign)
            {
                Ret.Status = ResultStatus.SignErr;
                Ret.Message = "簽名有誤";

                return Ret;
            }

            withdrawalModel = PayDB.GetWithdrawalByWithdrawID(body.WithdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();

            #region 單號檢查

            if (!(withdrawalModel != null && withdrawalModel.WithdrawID != 0))
            {
                Ret.Status = ResultStatus.Invalidate;
                Ret.Message = "查無此單";

                return Ret;
            }

            #endregion

            #region 單號狀態檢查

            if (withdrawalModel.FloatType == 0)
            {
                Ret.Status = ResultStatus.Invalidate;
                Ret.Message = "此單為後台提現單,無法發送API";

                return Ret;
            }

            #endregion
            switch (withdrawalModel.Status)
            {
                case 2:
                    WithdrawResultStatus = GatewayCommon.WithdrawResultStatus.Successs;
                    break;
                case 3:
                    WithdrawResultStatus = GatewayCommon.WithdrawResultStatus.Failure;
                    break;
                case 14:
                    WithdrawResultStatus = GatewayCommon.WithdrawResultStatus.ProblemWithdraw;
                    break;
                default:
                    WithdrawResultStatus = GatewayCommon.WithdrawResultStatus.WithdrawProgress;
                    break;
            }

            gpayReturn.SetByWithdraw(withdrawalModel, WithdrawResultStatus);

            System.Data.DataTable CompanyDT = PayDB.GetCompanyByID(withdrawalModel.forCompanyID, true);
            if (!(CompanyDT != null && CompanyDT.Rows.Count > 0))
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "找不到此商户资讯";

                return Ret;
            }

            GatewayCommon.Company CompanyModel = GatewayCommon.ToList<GatewayCommon.Company>(CompanyDT).FirstOrDefault();
            
            if (body.isReSendWithdraw)
            {
     
                //经过代理server 回调
                if (CompanyModel.IsProxyCallBack == 0)
                {
                    //發送一次回調 補單用
                    if (GatewayCommon.ReturnCompanyByWithdraw3(30, gpayReturn, withdrawalModel.ProviderCode))
                    {
                        //修改下游狀態
                        PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                    }
                }
                else {
                    //發送一次回調 補單用
                    if (GatewayCommon.ReturnCompanyByWithdraw2(30, gpayReturn, withdrawalModel.ProviderCode))
                    {
                        //修改下游狀態
                        PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                    }
                }
            }
            else
            {
                //發送三次回調(後台手動發款後用)
                if (CompanyModel.IsProxyCallBack == 0)
                {
                    //發送一次回調 補單用
                    if (GatewayCommon.ReturnCompanyByWithdraw4(30, gpayReturn, withdrawalModel.ProviderCode))
                    {
                        //修改下游狀態
                        PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                    }
                }
                else
                {
                    //發送一次回調 補單用
                    if (GatewayCommon.ReturnCompanyByWithdraw(30, gpayReturn, withdrawalModel.ProviderCode))
                    {
                        //修改下游狀態
                        PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                    }
                }
            }

            #endregion
        }
        catch (Exception ex)
        {
            Ret.Message = ex.Message;
            throw;
        }



        return Ret;
    }

    [HttpPost]
    [ActionName("QueryWithdraw")]
    public APIResult QueryWithdraw([FromBody] FromBodyReSendWithdraw body)
    {
        GatewayCommon.Withdrawal withdrawalModel;
        GatewayCommon.GPayReturnByWithdraw gpayReturn = new GatewayCommon.GPayReturnByWithdraw() { SetByWithdrawRetunData = new GatewayCommon.SetByWithdrawRetunData() };
        APIResult Ret = new APIResult();
        string withdrawSerial = "";
        bool SendCompanyReturn = false;
        GatewayCommon.WithdrawalByProvider withdrawReturn;
        try
        {
            #region SignCheck
            string strSign;
            string sign;

            #endregion

            #region 檢查Sign

            strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
                                    , body.WithdrawSerial
                                    , Pay.GPayBackendKey
                                    );

            sign = CodingControl.GetSHA256(strSign);

            if (sign != body.Sign)
            {
                Ret.Status = ResultStatus.SignErr;
                Ret.Message = "簽名有誤";

                return Ret;
            }


            #endregion

            withdrawalModel = PayDB.GetWithdrawalByWithdrawID(body.WithdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();

            #region 單號檢查

            if (!(withdrawalModel != null && withdrawalModel.WithdrawID != 0))
            {
                Ret.Status = ResultStatus.Invalidate;
                Ret.Message = "查無此單";
                return Ret;
            }

            #endregion

            #region 單號狀態檢查
            if (!(withdrawalModel.WithdrawType == 1 && withdrawalModel.Status == 1))
            {
                Ret.Status = ResultStatus.Invalidate;
                Ret.Message = "当前订单状态无法查询";
                return Ret;
            }
            #endregion

            withdrawSerial = withdrawalModel.WithdrawSerial;

            withdrawReturn = GatewayCommon.QueryWithdrawalByProvider(withdrawalModel);

            GatewayCommon.WithdrawResultStatus returnStatus = GatewayCommon.WithdrawResultStatus.WithdrawProgress;
            //0=成功/1=失败/2=审核中
            if (withdrawReturn.WithdrawalStatus == 0)
            {
                //2代表已成功且扣除額度,避免重複上分
                if (withdrawalModel.UpStatus != 2)
                {
                    //不修改Withdraw之狀態，預存中調整
                    PayDB.UpdateWithdrawSerialByUpData(2, withdrawReturn.ProviderReturn, withdrawReturn.UpOrderID, withdrawReturn.Amount, withdrawSerial);
                    var intReviewWithdrawal = PayDB.ReviewWithdrawal(withdrawSerial);
                    switch (intReviewWithdrawal)
                    {
                        case 0:
                            PayDB.InsertPaymentTransferLog("订单完成,尚未通知商户", 4, withdrawSerial, withdrawReturn.ProviderCode);
                            returnStatus = GatewayCommon.WithdrawResultStatus.Successs;
                            SendCompanyReturn = true;
                            break;
                        default:
                            //調整訂單為系統失敗單
                            PayDB.UpdateWithdrawStatus(14, withdrawSerial);
                            PayDB.InsertPaymentTransferLog("订单有误,系统问题单:" + intReviewWithdrawal, 4, withdrawSerial, withdrawReturn.ProviderCode);
                            returnStatus = GatewayCommon.WithdrawResultStatus.ProblemWithdraw;
                            break;
                    }
                }
                else
                {

                    if (withdrawalModel.Status == 2)
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.Successs;
                    }
                    else if (withdrawalModel.Status == 3)
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.Failure;
                    }
                    else if (withdrawalModel.Status == 14)
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.ProblemWithdraw;
                    }
                    else
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.WithdrawProgress;
                    }
                    PayDB.InsertPaymentTransferLog("订单完成,尚未通知商户", 4, withdrawSerial, withdrawReturn.ProviderCode);
                    SendCompanyReturn = true;

                }
            }
            else if (withdrawReturn.WithdrawalStatus == 1)
            {
                if (withdrawalModel.UpStatus != 2)
                {
                    PayDB.InsertPaymentTransferLog("订单完成,供应商通知此单失败,尚未通知商户", 4, withdrawSerial, withdrawReturn.ProviderCode);
             
                    PayDB.UpdateWithdrawSerialByUpData(2, withdrawReturn.ProviderReturn, withdrawReturn.UpOrderID, withdrawReturn.Amount, withdrawSerial);

                    returnStatus = GatewayCommon.WithdrawResultStatus.Failure; 
                }
                else
                {
                    if (withdrawalModel.Status == 2)
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.Successs;
                    }
                    else if (withdrawalModel.Status == 3)
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.Failure;
                    }
                    else if (withdrawalModel.Status == 14)
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.ProblemWithdraw;
                    }
                    else
                    {
                        returnStatus = GatewayCommon.WithdrawResultStatus.WithdrawProgress;
                    }
                    PayDB.InsertPaymentTransferLog("订单完成,供应商通知此单失败,尚未通知商户", 4, withdrawSerial, withdrawReturn.ProviderCode);
                }

                SendCompanyReturn = true;
            }
            else if (withdrawReturn.WithdrawalStatus == 2)
            {
                Ret.Status = ResultStatus.OK;
                Ret.Message = "上游审核中";

            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "查询失败";
            }
            if (SendCompanyReturn)
            {
                if (withdrawReturn.WithdrawalStatus == 0 || withdrawReturn.WithdrawalStatus == 1)
                {
                    withdrawalModel = PayDB.GetWithdrawalByWithdrawID(withdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();
                    //取得傳送資料
                    gpayReturn.SetByWithdraw(withdrawalModel, returnStatus);
                    //發送API 回傳商戶
                    if (withdrawalModel.FloatType != 0)
                    {
                        System.Data.DataTable CompanyDT = PayDB.GetCompanyByID(withdrawalModel.forCompanyID, true);
                        GatewayCommon.Company CompanyModel = GatewayCommon.ToList<GatewayCommon.Company>(CompanyDT).FirstOrDefault();
                        //發送三次回調(後台手動發款後用)
                        if (CompanyModel.IsProxyCallBack == 0)
                        {
                            //發送一次回調 補單用
                            if (GatewayCommon.ReturnCompanyByWithdraw4(30, gpayReturn, withdrawalModel.ProviderCode))
                            {
                                //修改下游狀態
                                PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                            }
                        }
                        else
                        {
                            //發送一次回調 補單用
                            if (GatewayCommon.ReturnCompanyByWithdraw(30, gpayReturn, withdrawalModel.ProviderCode))
                            {
                                //修改下游狀態
                                PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                            }
                        }
                    }
                }
            }
            

        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "查询失败:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("SendWithdraw")]
    public APIResult SendWithdraw([FromBody] FromBodySendWithdrawal body)
    {
        GatewayCommon.Withdrawal withdrawalModel;
        GatewayCommon.GPayReturnByWithdraw gpayReturn = new GatewayCommon.GPayReturnByWithdraw() { SetByWithdrawRetunData = new GatewayCommon.SetByWithdrawRetunData() };
        APIResult Ret = new APIResult() { Status= ResultStatus.ERR };
        string withdrawSerial = "";
        GatewayCommon.ReturnWithdrawByProvider withdrawReturn;        
        try
        {
            #region 檢查Sign
            string strSign;
            string sign;
            strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
                                    , body.WithdrawSerial
                                    , Pay.GPayBackendKey
                                    );

            sign = CodingControl.GetSHA256(strSign);

            if (sign != body.Sign)
            {
                Ret.Status = ResultStatus.SignErr;
                Ret.Message = "簽名有誤";

                return Ret;
            }

            withdrawalModel = PayDB.GetWithdrawalByWithdrawID(body.WithdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();

            #region 單號檢查

            if (!(withdrawalModel != null && withdrawalModel.WithdrawID != 0))
            {
                Ret.Status = ResultStatus.Invalidate;
                Ret.Message = "查無此單";

                return Ret;
            }

            #endregion

            withdrawSerial = withdrawalModel.WithdrawSerial;
    
            withdrawReturn = GatewayCommon.SendWithdraw(withdrawalModel);
            //SendStatus; 0=申請失敗/1=申請成功/2=交易已完成
            if (withdrawReturn.SendStatus == 1)
            {   //修改状态为上游审核中
                PayDB.UpdateWithdrawUpStatus(1, withdrawSerial);
                Ret.Status = ResultStatus.OK;
                Ret.Message = "上游審核中";
            }
            else if (withdrawReturn.SendStatus == 2)
            {
                //先將訂單改為進行中
                PayDB.UpdateWithdrawUpStatus(1, withdrawalModel.WithdrawSerial);
                Ret.Message = withdrawReturn.ReturnResult;
                GatewayCommon.WithdrawResultStatus returnStatus;
                TigerPayWithdrawData NotifyBody = JsonConvert.DeserializeObject<TigerPayWithdrawData>(withdrawReturn.ReturnResult);
                if (NotifyBody.result == "00" && NotifyBody.status.ToUpper() == "OK")
                {
                    withdrawalModel = PayDB.GetWithdrawalByWithdrawID(withdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();
                    //2代表已成功且扣除額度,避免重複上分
                    if (withdrawalModel.UpStatus != 2)
                    {
                        //不修改Withdraw之狀態，預存中調整
                        PayDB.UpdateWithdrawSerialByUpData(2, JsonConvert.SerializeObject(NotifyBody), NotifyBody.transaction_number, decimal.Parse(NotifyBody.amount), withdrawSerial);
                        var intReviewWithdrawal = PayDB.ReviewWithdrawal(withdrawSerial);
                        switch (intReviewWithdrawal)
                        {
                            case 0:
                                Ret.Status = ResultStatus.OK;
                                Ret.Message = "訂單完成";
                                PayDB.InsertPaymentTransferLog("订单完成,尚未通知商户", 4, withdrawSerial, withdrawalModel.ProviderCode);
                                returnStatus = GatewayCommon.WithdrawResultStatus.Successs;
                                break;
                            default:
                                //調整訂單為系統失敗單
                                PayDB.UpdateWithdrawStatus(14, withdrawSerial);
                                PayDB.InsertPaymentTransferLog("订单有误,系统问题单:" + intReviewWithdrawal, 4, withdrawSerial, withdrawalModel.ProviderCode);
                                returnStatus = GatewayCommon.WithdrawResultStatus.ProblemWithdraw;
                                break;
                        }
                    }
                    else
                    {

                        if (withdrawalModel.Status == 2)
                        {
                            returnStatus = GatewayCommon.WithdrawResultStatus.Successs;
                        }
                        else if (withdrawalModel.Status == 3)
                        {
                            returnStatus = GatewayCommon.WithdrawResultStatus.Failure;
                        }
                        else if (withdrawalModel.Status == 14)
                        {
                            returnStatus = GatewayCommon.WithdrawResultStatus.ProblemWithdraw;
                        }
                        else
                        {
                            returnStatus = GatewayCommon.WithdrawResultStatus.WithdrawProgress;
                        }
                        PayDB.InsertPaymentTransferLog("订单完成,尚未通知商户", 4, withdrawSerial, withdrawalModel.ProviderCode);

                    }

                    withdrawalModel = PayDB.GetWithdrawalByWithdrawID(withdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();
                    //取得傳送資料
                    gpayReturn.SetByWithdraw(withdrawalModel, returnStatus);
                    //發送API 回傳商戶
                    if (withdrawalModel.FloatType != 0)
                    {
                        System.Data.DataTable CompanyDT = PayDB.GetCompanyByID(withdrawalModel.forCompanyID, true);
                        GatewayCommon.Company CompanyModel = GatewayCommon.ToList<GatewayCommon.Company>(CompanyDT).FirstOrDefault();
                        //發送三次回調(後台手動發款後用)
                        if (CompanyModel.IsProxyCallBack == 0)
                        {
                            //發送一次回調 補單用
                            if (GatewayCommon.ReturnCompanyByWithdraw4(30, gpayReturn, withdrawalModel.ProviderCode))
                            {
                                //修改下游狀態
                                PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                            }
                        }
                        else
                        {
                            //發送一次回調 補單用
                            if (GatewayCommon.ReturnCompanyByWithdraw(30, gpayReturn, withdrawalModel.ProviderCode))
                            {
                                //修改下游狀態
                                PayDB.UpdateWithdrawSerialByStatus(2, withdrawalModel.WithdrawID);
                            }
                        }
                    }
                }
        
            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = withdrawReturn.ReturnResult;
            }

            #endregion
        }
        catch (Exception ex)
        {
            Ret.Status = ResultStatus.ERR;
            Ret.Message = ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("GetAutoDistributionGroupWithdraw")]
    public APIResult GetAutoDistributionGroupWithdraw([FromBody] FromBodyReSendWithdraw body)
    {

        AutoDistributionGroupWithdrawData Ret = new AutoDistributionGroupWithdrawData();
        System.Data.DataTable DT;
        List<string> Withdrawals=new List<string>();
        try
        {
            //#region SignCheck
            //string strSign;
            //string sign;

            //#endregion

            //#region 檢查Sign

            //strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
            //                        , body.WithdrawSerial
            //                        , Pay.GPayBackendKey
            //                        );

            //sign = CodingControl.GetSHA256(strSign);

            //if (sign != body.Sign)
            //{
            //    Ret.Status = ResultStatus.SignErr;
            //    Ret.Message = "簽名有誤";

            //    return Ret;
            //}

            //#endregion

   
            DT = PayDB.GetAutoDistributionGroupWithdraw();

            if (!(DT != null && DT.Rows.Count > 0))
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
                return Ret;
            }

            for (int i = 0; i < DT.Rows.Count; i++)
            {
                Withdrawals.Add(DT.Rows[i]["WithdrawSerial"].ToString());
            }

            Ret.WithdrawSerials = Withdrawals;
            Ret.Status = ResultStatus.OK;
        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Search Fail:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("AutoDistributionGroupWithdraw")]
    public APIResult AutoDistributionGroupWithdraw([FromBody] FromBodyReSendWithdraw body)
    {
        GatewayCommon.Withdrawal withdrawalModel;
        GatewayCommon.GPayReturnByWithdraw gpayReturn = new GatewayCommon.GPayReturnByWithdraw() { SetByWithdrawRetunData = new GatewayCommon.SetByWithdrawRetunData() };
        APIResult Ret = new APIResult();
        GatewayCommon.Company CompanyModel;
        System.Data.DataTable DT;
        int GroupID = 0;
        int spUpdateProxyProviderOrderGroupReturn = -8;
        try
        {

            withdrawalModel = PayDB.GetWithdrawalByWithdrawID(body.WithdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();
       
            #region 單號狀態檢查
            if (!(withdrawalModel.HandleByAdminID == 0 && withdrawalModel.Status == 1))
            {
                Ret.Status = ResultStatus.Invalidate;
                Ret.Message = "Another Withdrawing";
                return Ret;
            }
            #endregion

            DT = RedisCache.Company.GetCompanyByID(withdrawalModel.forCompanyID);

            #region 公司檢查
            if (!(DT != null && DT.Rows.Count > 0))
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "Company Not Exist";
                return Ret;
            }
            //DT = PayDB.GetCompanyByCode(body.CompanyCode, true);


            CompanyModel = GatewayCommon.ToList<GatewayCommon.Company>(DT).FirstOrDefault();
            #endregion
            if (CompanyModel.ProviderGroups != "0")
            {
                GroupID = GatewayCommon.SelectProxyProviderGroupByCompanySelected(withdrawalModel.ProviderCode, withdrawalModel.Amount, CompanyModel.ProviderGroups);
            }
            else
            {
                GroupID = GatewayCommon.SelectProxyProviderGroup(withdrawalModel.ProviderCode, withdrawalModel.Amount);
            }

            if (GroupID != 1)
            {
                spUpdateProxyProviderOrderGroupReturn = PayDB.spUpdateProxyProviderOrderGroupByAdmin(withdrawalModel.WithdrawSerial, GroupID);

                switch (spUpdateProxyProviderOrderGroupReturn)
                {
                    case 0:
                        Ret.Status = ResultStatus.OK;
                        Ret.Message = "Success";
                        break;
                    case -1:
                        Ret.Status = ResultStatus.ERR;
                        Ret.Message = "Lock Fail";
                        break;
                    case -2:
                        Ret.Status = ResultStatus.ERR;
                        Ret.Message = "Another Withdrawing (sp)";
                        break;
                    default:
                        break;
                }
            }
            else {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Group Can Accept Order";
            }     
        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Exception:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("SetSummaryCompanyByHour")]
    public APIResult SetSummaryCompanyByHour()
    {

        APIResult Ret = new APIResult();
        System.Data.DataTable DT;
        List<string> Withdrawals = new List<string>();
        int DBreturn = -8;
        try
        {
            //#region SignCheck
            //string strSign;
            //string sign;

            //#endregion

            //#region 檢查Sign

            //strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
            //                        , body.WithdrawSerial
            //                        , Pay.GPayBackendKey
            //                        );

            //sign = CodingControl.GetSHA256(strSign);

            //if (sign != body.Sign)
            //{
            //    Ret.Status = ResultStatus.SignErr;
            //    Ret.Message = "簽名有誤";

            //    return Ret;
            //}

            //#endregion


            DBreturn = PayDB.SetSummaryCompanyByHour();

            if (DBreturn == 0)
            {
                Ret.Status = ResultStatus.OK;
            }
            else {
                Ret.Status = ResultStatus.ERR;
            }
        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Search Fail:" + ex.Message;
            throw;
        }
        return Ret;
    }


    [HttpPost]
    [ActionName("QueryingInProcessWithdrawalOrders")]
    public WithdrawalAPIResult QueryingInProcessWithdrawalOrders()
    {
        List< Withdrawal> withdrawalModels;
        WithdrawalAPIResult Ret = new WithdrawalAPIResult();
 
        try
        {
            var withdrawalOrdersDT = PayDB.QueryingInProcessWithdrawalOrders();
            if (withdrawalOrdersDT != null&& withdrawalOrdersDT.Rows.Count>0 )
            {
                withdrawalModels = withdrawalOrdersDT.ToList<Withdrawal>().ToList();
                Ret.Withdrawals = withdrawalModels;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }
            else {
              
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
            }

        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("QueryingInProcessOrders")]
    public OrderAPIResult QueryingInProcessOrders()
    {
        List<Withdrawal> withdrawalModels=null;
        List<Deposite> depositModels=null;
        OrderAPIResult Ret = new OrderAPIResult();
        DateTime EndDate = DateTime.UtcNow;
        DateTime StartDate = EndDate.AddHours(-1); // 過去一小時

        try
        {
            var withdrawalOrdersDT = PayDB.QueryingInProcessWithdrawalOrders();
            var depositOrdersDT = PayDB.QueryingInProcessDepositeOrders(StartDate, EndDate);
            if ((withdrawalOrdersDT != null && withdrawalOrdersDT.Rows.Count > 0)|| (depositOrdersDT != null && depositOrdersDT.Rows.Count > 0))
            {
                withdrawalModels = withdrawalOrdersDT.ToList<Withdrawal>().ToList();
                depositModels = depositOrdersDT.ToList<Deposite>().ToList();
                Ret.Withdrawals = withdrawalModels;
                Ret.Deposites = depositModels;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }
            else
            {

                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
            }

        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("GetSuccessOrderByDate")]
    public GetOrderByDateResult GetSuccessOrderByDate([FromBody] FromBodyGetOrder body)
    {
        List<Withdrawal> withdrawalModels;
        List<Deposite> depositeModels;
        GetOrderByDateResult Ret = new GetOrderByDateResult();
        Ret.Status = ResultStatus.ERR;
        Ret.Message = "No Data";
        body.StartDate = body.StartDate.Date;
        body.EndDate = body.StartDate.Date.AddDays(1);

        try
        {
            var withdrawalOrdersDT = PayDB.QueryingSuccessWithdrawalOrders(body.StartDate,body.EndDate,body.CompanyCode);
            if (withdrawalOrdersDT != null && withdrawalOrdersDT.Rows.Count > 0)
            {
                withdrawalModels = withdrawalOrdersDT.ToList<Withdrawal>().ToList();
                Ret.Withdrawals = withdrawalModels;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }


            var depositeOrdersDT = PayDB.QueryingSuccessDepositeOrders(body.StartDate, body.EndDate, body.CompanyCode);
            if (depositeOrdersDT != null && depositeOrdersDT.Rows.Count > 0)
            {
                depositeModels = depositeOrdersDT.ToList<Deposite>().ToList();
                Ret.Deposites = depositeModels;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }



        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("GetOrderByOrderNumber")]
    public GetOrderByOrderNumberResult GetOrderByOrderNumber([FromBody] FromBodyGetOrder body)
    {
        Withdrawal withdrawalModel;
        Deposite depositeModel;
        GetOrderByOrderNumberResult Ret = new GetOrderByOrderNumberResult();
        Ret.Status = ResultStatus.ERR;
        Ret.Message = "No Data";
        try
        {
            string order = body.OrderNumber;
            string prefix = order.Substring(0, 2); // 擷取前兩個字串

            if (prefix == "OP")
            {
                var withdrawalOrdersDT = PayDB.QueryingWithdrawalOrder(body.OrderNumber,body.CompanyCode);
                if (withdrawalOrdersDT != null && withdrawalOrdersDT.Rows.Count > 0)
                {
                    withdrawalModel = withdrawalOrdersDT.ToList<Withdrawal>().ToList().First();
                    Ret.Withdrawal = withdrawalModel;
                    Ret.Status = ResultStatus.OK;
                    Ret.Message = "SUCCESS";
                }
            }
            else if (prefix == "IP")
            {

                var depositeOrdersDT = PayDB.QueryingDepositeOrder(body.OrderNumber, body.CompanyCode);
                if (depositeOrdersDT != null && depositeOrdersDT.Rows.Count > 0)
                {
                    depositeModel = depositeOrdersDT.ToList<Deposite>().ToList().First();
                    Ret.Deposite = depositeModel;
                    Ret.Status = ResultStatus.OK;
                    Ret.Message = "SUCCESS";
                }
            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
            }
        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("WithdrawalReview")]
    public APIResult WithdrawalReview([FromBody] FromBodyReview body)
    {
        GatewayCommon.Withdrawal withdrawalModel;
        APIResult Ret = new APIResult() { Status = ResultStatus.ERR };
        GatewayCommon.WithdrawLimit CompanyWithdrawLimitModel;
        System.Data.DataTable ProviderWithdrawLimitDT;
        System.Data.DataTable CompanyWithdrawLimitDT;
        GatewayCommon.WithdrawLimit withdrawLimitModel;
        decimal UpCharge = 0;
        decimal UpRate = 0;

        string ProviderCode = Pay.WithdrawalProviderCode;
        string ServiceType = Pay.WithdrawalServiceType;


        if (string.IsNullOrWhiteSpace(body.WithdrawSerial))
        {
            Ret.Message = "缺少必要參數";
            return Ret;
        }

        string lockKey = "WithdrawalReview:" + body.WithdrawSerial;
        bool locked = false;
        try
        {
            locked = RedisCache.GetLocker(lockKey, 30, () =>
            {
                #region 檢查Sign

                withdrawalModel = PayDB.GetWithdrawalByWithdrawID(body.WithdrawSerial).ToList<GatewayCommon.Withdrawal>().FirstOrDefault();


                if (withdrawalModel == null)
                {
                    Ret.Message = "訂單不存在";
                    return;
                }

                System.Data.DataTable CompanyDT = PayDB.GetCompanyByID(withdrawalModel.forCompanyID, true);
                if (CompanyDT == null || CompanyDT.Rows.Count == 0)
                {
                    Ret.Message = "商戶不存在";
                    return;
                }

                if (withdrawalModel.Status == 2 || withdrawalModel.Status == 3)
                {
                    Ret.Message = "訂單已審核完成";
                    return;
                }

                if (withdrawalModel.Status == 1)
                {
                    Ret.Message = "訂單審核中";
                    return;
                }

                if (body.Status == 1) //列為成功
                {
                    if (withdrawalModel.FloatType == 0)//後台申請訂單
                    {
                        ProviderWithdrawLimitDT = RedisCache.ProviderWithdrawLimit.GetProviderBackendWithdrawLimit(ProviderCode, withdrawalModel.CurrencyType);
                    }
                    else
                    { //API 申請訂單
                        ProviderWithdrawLimitDT = RedisCache.ProviderWithdrawLimit.GetProviderAPIWithdrawLimit(ProviderCode, withdrawalModel.CurrencyType);
                    }

                    if (ProviderWithdrawLimitDT != null && ProviderWithdrawLimitDT.Rows.Count > 0)
                    {
                        withdrawLimitModel = GatewayCommon.ToList<GatewayCommon.WithdrawLimit>(ProviderWithdrawLimitDT).FirstOrDefault();
                    }
                    else
                    {
                        Ret.Message = "尚未設定供應商手續費";
                        return;
                    }

                    UpCharge = withdrawLimitModel.Charge;
                    UpRate = withdrawLimitModel.Rate;


                    //检查供应商通道额度
                    var ProviderPointModel = PayDB.GetProviderPointByProviderCode(ProviderCode, withdrawalModel.CurrencyType);

                    if (ProviderPointModel != null && ProviderPointModel.Rows.Count > 0)
                    {
                        if (withdrawalModel.Amount + UpCharge + (UpRate * 0.01M * withdrawalModel.Amount) > decimal.Parse(ProviderPointModel.Rows[0]["SystemPointValue"].ToString()))
                        {
                            Ret.Message = "供應商通道額度不足";
                            return;
                        }
                    }
                    else
                    {
                        Ret.Message = "取得供應商餘額失敗";
                        return;
                    }

                    //检查商户支付通道额度
                    if (withdrawalModel.FloatType == 0)//後台申請訂單
                    {
                        CompanyWithdrawLimitDT = RedisCache.CompanyWithdrawLimit.GetCompanyBackendtWithdrawLimit(withdrawalModel.forCompanyID, withdrawalModel.CurrencyType);
                    }
                    else
                    { //API 申請訂單
                        CompanyWithdrawLimitDT = RedisCache.CompanyWithdrawLimit.GetCompanyAPIWithdrawLimit(withdrawalModel.forCompanyID, withdrawalModel.CurrencyType);
                    }

                    if (CompanyWithdrawLimitDT != null && CompanyWithdrawLimitDT.Rows.Count > 0)
                    {
                        CompanyWithdrawLimitModel = GatewayCommon.ToList<GatewayCommon.WithdrawLimit>(CompanyWithdrawLimitDT).FirstOrDefault();
                        if (CompanyWithdrawLimitModel.MaxLimit == 0 || CompanyWithdrawLimitModel.MinLimit == 0)
                        {
                            Ret.Message = "尚未設定商戶支付通道限額";
                            return;
                        }

                        withdrawalModel.DownRate = CompanyWithdrawLimitModel.Rate;
                        withdrawalModel.CollectCharge = CompanyWithdrawLimitModel.Charge;
                    }
                    else
                    {
                        Ret.Message = "尚未設定商戶手續費";
                        return;
                    }


                    var CompanyPointModel = PayDB.GetServiceCompanyPoint(withdrawalModel.forCompanyID, withdrawalModel.CurrencyType, ServiceType);

                    if (CompanyPointModel != null && CompanyPointModel.Rows.Count > 0)
                    {
                        if (withdrawalModel.Amount + CompanyWithdrawLimitModel.Charge + (CompanyWithdrawLimitModel.Rate * 0.01M * withdrawalModel.Amount) > decimal.Parse(CompanyPointModel.Rows[0]["SystemPointValue"].ToString()))
                        {
                            Ret.Message = "商戶支付通道額度不足";
                            return;
                        }
                    }
                    else
                    {
                        Ret.Message = "取得商戶通道餘額失敗";
                        return;
                    }

                    //修改訂單狀態為審核中
                    int DBreturn = PayDB.UpdateWithdrawalToProcess(ProviderCode, ServiceType, UpCharge, UpRate, withdrawalModel.WithdrawSerial);

                    if (DBreturn == 0)
                    {
                        Ret.Message = "訂單已在處理中";
                        return;
                    }

                    DBreturn = PayDB.UpdateWithdrawalToSuccess(withdrawalModel.WithdrawSerial);


                    if (DBreturn == 0)
                    {
                        if (withdrawalModel.FloatType != 0)
                        {
                            string strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
                                        , withdrawalModel.WithdrawSerial
                                        , Pay.GPayBackendKey
                                        );

                            string sign = CodingControl.GetSHA256(strSign);

                            ReSendWithdraw(new FromBodyReSendWithdraw()
                            {
                                WithdrawSerial = withdrawalModel.WithdrawSerial,
                                Sign = sign,
                                isReSendWithdraw = false
                            });
                        }

                        Ret.Message = "SUCCESS";
                        Ret.Status = ResultStatus.OK;
                        return;
                    }
                    else
                    {
                        Ret.Message = "訂單處理失敗:" + DBreturn;
                        return;
                    }
                }
                else if (body.Status == -1) //更改為失敗單
                {
                    int DBreturn = PayDB.UpdateWithdrawalToFail(withdrawalModel.WithdrawSerial);

                    if (DBreturn == 0)
                    {
                        Ret.Message = "訂單已在處理中";
                        return;
                    }

                    if (withdrawalModel.FloatType != 0)
                    {
                        string strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
                                    , withdrawalModel.WithdrawSerial
                                    , Pay.GPayBackendKey
                                    );

                        string sign = CodingControl.GetSHA256(strSign);

                        ReSendWithdraw(new FromBodyReSendWithdraw()
                        {
                            WithdrawSerial = withdrawalModel.WithdrawSerial,
                            Sign = sign,
                            isReSendWithdraw = false
                        });
                    }


                    Ret.Message = "SUCCESS";
                    Ret.Status = ResultStatus.OK;

                    return;
                }
                #endregion
            });

            // 如果鎖沒取得，代表其他人正在處理中
            if (!locked)
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "訂單正在處理中，請稍後再試";
            }
        }
        catch (Exception ex)
        {
            Ret.Status = ResultStatus.ERR;
            Ret.Message = ex.Message;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("PaymentReview")]
    public APIResult PaymentReview([FromBody] FromBodyReview body)
    {
        GatewayCommon.Payment paymentModel;
        APIResult Ret = new APIResult() { Status = ResultStatus.ERR };

        if (string.IsNullOrWhiteSpace(body.PaymentSerial))
        {
            Ret.Message = "缺少必要參數";
            return Ret;
        }

        string lockKey = "PaymentReviewLock:" + body.PaymentSerial;
        bool locked = false;

        try
        {
            locked = RedisCache.GetLocker(lockKey, 30, () =>
            {
                #region 檢查Sign
                paymentModel = PayDB.GetPaymentByPaymentID(body.PaymentSerial).ToList<GatewayCommon.Payment>().FirstOrDefault();


                if (paymentModel == null)
                {
                    Ret.Message = "訂單不存在";
                    return;
                }

                System.Data.DataTable CompanyDT = PayDB.GetCompanyByID(paymentModel.forCompanyID, true);
                if (CompanyDT == null || CompanyDT.Rows.Count == 0)
                {
                    Ret.Message = "商戶不存在";
                    return;
                }

                if (paymentModel.ProcessStatus == 2 || paymentModel.ProcessStatus == 3 || paymentModel.ProcessStatus == 4)
                {
                    Ret.Message = "訂單已審核完成";
                    return;
                }

                if (paymentModel.ProcessStatus != 1)
                {
                    Ret.Message = "訂單處理中";
                    return;
                }

                int DBreturn = PayDB.ReviewPaymentToSuccess(paymentModel, body.RealAmount);

                if (DBreturn == 0)
                {
                    string strSign = string.Format("PaymentSerial={0}&GPayBackendKey={1}"
                         , paymentModel.PaymentSerial
                         , Pay.GPayBackendKey
                         );

                    string sign = CodingControl.GetSHA256(strSign);

                    ReSendPayment(new FromBodyReSendPayment()
                    {
                        PaymentSerial = paymentModel.PaymentSerial,
                        Sign = sign
                    });

                    Ret.Message = "SUCCESS";
                    Ret.Status = ResultStatus.OK;
                    return;
                }
                else
                {
                    Ret.Message = "訂單處理失敗:" + DBreturn;
                    return;
                }

                #endregion
            });

            // 如果鎖沒取得，代表其他人正在處理中
            if (!locked)
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "訂單正在處理中，請稍後再試";
            }
    
        }
        catch (Exception ex)
        {
            Ret.Status = ResultStatus.ERR;
            Ret.Message = ex.Message;
        }

        return Ret;
    }

    //[HttpPost]
    //[ActionName("QueryingSummaryCompanyByDate")]
    //public GetSummaryCompanyByDateResult QueryingSummaryCompanyByDate([FromBody] FromBodyGetOrder body)
    //{
    //    List<SummaryCompanyByDate> SummaryCompanyByDates;

    //    GetSummaryCompanyByDateResult Ret = new GetSummaryCompanyByDateResult();

    //    try
    //    {

    //        body.StartDate = body.StartDate.Date;
    //        body.EndDate = body.EndDate.Date;

    //        var DT = PayDB.QueryingSummaryCompanyByDate(body.StartDate, body.EndDate, body.CompanyCode);
    //        if (DT != null && DT.Rows.Count > 0)
    //        {
    //            SummaryCompanyByDates = DT.ToList<SummaryCompanyByDate>().ToList();
    //            Ret.SummaryCompanyByDates= SummaryCompanyByDates;
    //            Ret.Status = ResultStatus.OK;
    //            Ret.Message = "SUCCESS";
    //        }
    //        else {
    //            Ret.Status = ResultStatus.ERR;
    //            Ret.Message = "No Data";
    //        }
    //    }
    //    catch (Exception ex)
    //    {

    //        Ret.Status = ResultStatus.ERR;
    //        Ret.Message = "Error:" + ex.Message;
    //        throw;
    //    }

    //    return Ret;
    //}


    [HttpPost]
    [ActionName("QueryingAllCompanyDate")]
    public GetAllCompanyDateResult QueryingAllCompanyDate([FromBody] FromBodyGetOrder body)
    {
        GetAllCompanyDateResult Ret = new GetAllCompanyDateResult();
        List<JObject> CompanyDates = new List<JObject>();
        try
        {
            var DT = PayDB.GetAllCompanyData();

            if (DT != null && DT.Rows.Count > 0)
            {
                foreach (DataRow DTRow in DT.Rows)
                {
                    JObject CompanyData = new JObject();

                    if (DTRow["CompanyCode"].ToString() == "VPayTest")
                    {
                        continue;
                    }

                    CompanyData["CompanyName"] = DTRow["CompanyName"].ToString();
                    CompanyData["CompanyCode"] = DTRow["CompanyCode"].ToString();
                    CompanyData["TimeZone"] = decimal.Parse(DTRow["TimeZone"].ToString());
                    CompanyDates.Add(CompanyData);
                }
                Ret.Datas = CompanyDates;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
            }
        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("GetCompanyBalanceByCompanyCode")]
    public GetCompanyArrayDateResult GetCompanyBalanceByCompanyCode([FromBody] FromBodyGetOrder body)
    {
        GetCompanyArrayDateResult Ret = new GetCompanyArrayDateResult();
        try
        {
            var DT = PayDB.GetCanUseCompanyPoint(body.CompanyCode);

            if (DT != null && DT.Rows.Count > 0)
            {
                JArray CompanyDataList = new JArray();

                foreach (DataRow DTRow in DT.Rows)
                {
                    JObject CompanyData = new JObject();

                    CompanyData["CompanyCode"] = body.CompanyCode;

                    object currencyTypeObj = DTRow["CurrencyType"];
                    CompanyData["CurrencyType"] = currencyTypeObj != null ? currencyTypeObj.ToString() : string.Empty;

                    object canUsePointObj = DTRow["CanUsePoint"];
                    decimal canUsePoint = 0;
                    if (canUsePointObj != null)
                    {
                        decimal.TryParse(canUsePointObj.ToString(), out canUsePoint);
                    }
                    CompanyData["CanUsePoint"] = canUsePoint;

                    CompanyDataList.Add(CompanyData);
                }

                Ret.Datas = CompanyDataList;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
            }
        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("GetCompanySummaryByCompanyCode")]
    public GetCompanyArrayDateResult GetCompanySummaryByCompanyCode([FromBody] FromBodyGetOrder body)
    {
        GetCompanyArrayDateResult Ret = new GetCompanyArrayDateResult();
        try
        {
            DateTime queryDateOnly = body.StartDate.Date;
            body.StartDate = body.StartDate.Date;
            body.EndDate = body.EndDate.Date.AddDays(1);

            var SummaryCompanyByDatesDT = PayDB.QueryingSummaryCompanyByCompanyCode(body.StartDate, body.EndDate, body.CompanyCode);

            if (SummaryCompanyByDatesDT != null && SummaryCompanyByDatesDT.Rows.Count > 0)
            {
                JArray SummaryList = new JArray();

                foreach (DataRow row in SummaryCompanyByDatesDT.Rows)
                {
                    JObject summaryData = new JObject();

                    summaryData["WithdrawalTotalCount"] = string.IsNullOrWhiteSpace(row["WithdrawalTotalCount"].ToString()) ? 0 : int.Parse(row["WithdrawalTotalCount"].ToString());
                    summaryData["WithdrawalSuccessCount"] = string.IsNullOrWhiteSpace(row["WithdrawalSuccessCount"].ToString()) ? 0 : int.Parse(row["WithdrawalSuccessCount"].ToString());
                    summaryData["WithdrawalSuccessAmount"] = string.IsNullOrWhiteSpace(row["WithdrawalSuccessAmount"].ToString()) ? 0 : decimal.Parse(row["WithdrawalSuccessAmount"].ToString());
                    summaryData["PaymentTotalCount"] = string.IsNullOrWhiteSpace(row["PaymentTotalCount"].ToString()) ? 0 : int.Parse(row["PaymentTotalCount"].ToString());
                    summaryData["PaymentSuccessCount"] = string.IsNullOrWhiteSpace(row["PaymentSuccessCount"].ToString()) ? 0 : int.Parse(row["PaymentSuccessCount"].ToString());
                    summaryData["PaymentSuccessAmount"] = string.IsNullOrWhiteSpace(row["PaymentSuccessAmount"].ToString()) ? 0 : decimal.Parse(row["PaymentSuccessAmount"].ToString());
                    summaryData["CurrencyType"] = row["CurrencyType"].ToString();
                    summaryData["SummaryDate"] = body.StartDate.Date.ToString("yyyy/M/d")+"-"+ body.EndDate.Date.ToString("yyyy/M/d");

                    SummaryList.Add(summaryData);
                }

                Ret.Datas = SummaryList;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }
            else
            {
                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
            }
        }
        catch (Exception ex)
        {
            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }


    [HttpPost]
    [ActionName("GetOrderByOrderNo")]
    public GetOrderByOrderNoResult GetOrderByOrderNo([FromBody] FromBodyGetOrder body)
    {
        GetOrderByOrderNoResult Ret = new GetOrderByOrderNoResult();
        Ret.Status = ResultStatus.ERR;
        Ret.Message = "No Data";
        bool needCb = false;
        try
        {
       
            var depositeOrdersDT = PayDB.QueryingDepositeOrder(body.OrderNumber);
            if (depositeOrdersDT != null && depositeOrdersDT.Rows.Count > 0)
            {
                var orderData = new Order();
                orderData.OrderNo = depositeOrdersDT.Rows[0]["PaymentSerial"].ToString();
                orderData.CompanyOrderNo = depositeOrdersDT.Rows[0]["OrderID"].ToString();
                orderData.CompanyName = depositeOrdersDT.Rows[0]["CompanyName"].ToString();
                orderData.CompanyCode = depositeOrdersDT.Rows[0]["CompanyCode"].ToString();
                orderData.CurrencyType = depositeOrdersDT.Rows[0]["CurrencyType"].ToString();
                orderData.ProviderName = depositeOrdersDT.Rows[0]["ProviderName"].ToString();
                orderData.Amount = decimal.Parse(depositeOrdersDT.Rows[0]["Amount"].ToString());
                orderData.RealAmount = decimal.Parse(depositeOrdersDT.Rows[0]["PartialOrderAmount"].ToString());
                if (depositeOrdersDT.Rows[0]["FinishDate"] != DBNull.Value &&
               !string.IsNullOrWhiteSpace(depositeOrdersDT.Rows[0]["FinishDate"].ToString()))
                {
                    orderData.FinishDate = DateTime.Parse(depositeOrdersDT.Rows[0]["FinishDate"].ToString());
                }
                else
                {
                    // 給個預設值或處理邏輯
                    orderData.FinishDate = DateTime.MinValue;  // 或其他預設值
                }
                orderData.CreateDate = DateTime.Parse(depositeOrdersDT.Rows[0]["CreateDate"].ToString());
                orderData.ServiceTypeName = depositeOrdersDT.Rows[0]["ServiceTypeName"].ToString();
                orderData.Timezone = decimal.Parse(depositeOrdersDT.Rows[0]["Timezone"].ToString());
                orderData.OrderType = "D";

                switch (depositeOrdersDT.Rows[0]["ProcessStatus"].ToString())
                {
                    case "2":
                    case "4":
                        needCb = true;
                        orderData.Status = "SUCCESS";
                        break;
                    default:
                        orderData.Status = "PENDDING";
                        break;
                }

                if (needCb && depositeOrdersDT.Rows[0]["ProcessStatus"].ToString() == "2")
                {
    

                    string strSign = string.Format("PaymentSerial={0}&GPayBackendKey={1}"
                             , orderData.OrderNo
                             , Pay.GPayBackendKey
                             );

                    string sign = CodingControl.GetSHA256(strSign);

                    ReSendPayment(new FromBodyReSendPayment()
                    {
                        PaymentSerial = orderData.OrderNo,
                        Sign = sign
                    });
                }


                Ret.Order = orderData;
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            } else
            {
                var withdrawalOrdersDT = PayDB.QueryingWithdrawalOrder(body.OrderNumber);
                if (withdrawalOrdersDT != null && withdrawalOrdersDT.Rows.Count > 0)
                {
                    var orderData = new Order();
                    orderData.OrderNo = withdrawalOrdersDT.Rows[0]["WithdrawSerial"].ToString();
                    orderData.CompanyOrderNo = withdrawalOrdersDT.Rows[0]["DownOrderID"].ToString();
                    orderData.CompanyName = withdrawalOrdersDT.Rows[0]["CompanyName"].ToString();
                    orderData.CompanyCode = withdrawalOrdersDT.Rows[0]["CompanyCode"].ToString();
                    orderData.CurrencyType = withdrawalOrdersDT.Rows[0]["CurrencyType"].ToString();
                    orderData.ProviderName = withdrawalOrdersDT.Rows[0]["ProviderName"].ToString();
                    orderData.Amount = decimal.Parse(withdrawalOrdersDT.Rows[0]["Amount"].ToString());
                    orderData.RealAmount = decimal.Parse(withdrawalOrdersDT.Rows[0]["Amount"].ToString());

                    if (withdrawalOrdersDT.Rows[0]["FinishDate"] != DBNull.Value &&
                    !string.IsNullOrWhiteSpace(withdrawalOrdersDT.Rows[0]["FinishDate"].ToString()))
                    {
                        orderData.FinishDate = DateTime.Parse(withdrawalOrdersDT.Rows[0]["FinishDate"].ToString());
                    }
                    else
                    {
                        // 給個預設值或處理邏輯
                        orderData.FinishDate = DateTime.MinValue;  // 或其他預設值
                    }
                    orderData.CreateDate = DateTime.Parse(withdrawalOrdersDT.Rows[0]["CreateDate"].ToString());
                    orderData.ServiceTypeName = withdrawalOrdersDT.Rows[0]["ServiceTypeName"].ToString();
                    orderData.Timezone = decimal.Parse(withdrawalOrdersDT.Rows[0]["Timezone"].ToString());
                    orderData.OrderType = "W";

                    switch (withdrawalOrdersDT.Rows[0]["Status"].ToString())
                    {
                        case "2":
                            orderData.Status = "SUCCESS";
                            needCb = true;
                            break;
                        case "3":
                            orderData.Status = "FAIL";
                            needCb = true;
                            break;
                        default:
                            orderData.Status = "PENDDING";
                            break;
                    }

                    if (needCb&& withdrawalOrdersDT.Rows[0]["DownStatus"].ToString()!="2")
                    {
                        string strSign = string.Format("WithdrawSerial={0}&GPayBackendKey={1}"
                                 , orderData.OrderNo
                                 , Pay.GPayBackendKey
                                 );

                        string sign = CodingControl.GetSHA256(strSign);

                        ReSendWithdraw(new FromBodyReSendWithdraw()
                        {
                            WithdrawSerial = orderData.OrderNo,
                            Sign = sign,
                            isReSendWithdraw = true
                        });
                    }


                    Ret.Order = orderData;
                    Ret.Status = ResultStatus.OK;
                    Ret.Message = "SUCCESS";
                }
                else {
                    Ret.Status = ResultStatus.ERR;
                    Ret.Message = "No Data";
                }
            }
        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("GetAllBlockChainRate")]
    public BlockChainRateAPIResult GetAllBlockChainRate()
    {
        BlockChainRateAPIResult Ret = new BlockChainRateAPIResult();

        try
        {
            var DT = RedisCache.BlockChainRate.GetAllBlockChainRate();

            if (DT != null)
            {
                Ret.BlockChainRates = GatewayCommon.ToList<BlockChainRate>(DT).ToList();
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }
            else
            {

                Ret.Status = ResultStatus.ERR;
                Ret.Message = "No Data";
            }

        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    [HttpPost]
    [ActionName("SetBlockChainRate")]
    public APIResult SetBlockChainRate([FromBody] BlockChainRate body)
    {
        APIResult Ret = new APIResult();

        try
        {
            var retSetBlockChainRate = PayDB.SetBlockChainRate(body.CurrencyType, body.Rate, body.UpFloatRate, body.DownFloatRate);

            if (retSetBlockChainRate>0)
            {
                Ret.Status = ResultStatus.OK;
                Ret.Message = "SUCCESS";
            }
            else
            {

                Ret.Status = ResultStatus.ERR;
                Ret.Message = "UPADTE FAIL";
            }

        }
        catch (Exception ex)
        {

            Ret.Status = ResultStatus.ERR;
            Ret.Message = "Error:" + ex.Message;
            throw;
        }

        return Ret;
    }

    public class WithdrawalAPIResult:APIResult
    {
       public List<Withdrawal> Withdrawals;
    }

    public class OrderAPIResult : APIResult
    {
        public List<Withdrawal> Withdrawals;
        public List<Deposite> Deposites;
    }

    public class GetOrderByDateResult : APIResult
    {
        public List<Withdrawal> Withdrawals;
        public List<Deposite> Deposites;
    }

    public class GetOrderByOrderNumberResult : APIResult
    {
        public Withdrawal Withdrawal;
        public Deposite Deposite;
    }

    public class GetOrderByOrderNoResult : APIResult
    {
        public Order Order;
    }


    

    public class GetSummaryCompanyByDateResult : APIResult
    {
        public List<JObject> CompanyDates;
    }

    public class GetAllCompanyDateResult : APIResult
    {
        public List<JObject> Datas;
    }

    public class GetCompanyDateResult : APIResult
    {
        public JObject Datas;
    }

    public class GetCompanyArrayDateResult : APIResult
    {
        public JArray Datas;
    }
    
    public class Order
    {
        public string OrderNo { get; set; } //系統訂單號
        public string CompanyOrderNo { get; set; } //商戶訂單號
        public string CompanyName { get; set; }  //商戶名稱
        public string CompanyCode { get; set; }  //商戶名稱
        public string CurrencyType { get; set; }  //商戶名稱
        public string ProviderName { get; set; } //供應商名稱
        public decimal Amount { get; set; } //訂單金額
        public decimal RealAmount { get; set; } //實際金額
        public DateTime FinishDate { get; set; } //訂單完成時間
        public DateTime CreateDate { get; set; } //訂單建立時間
        public string ServiceTypeName { get; set; }//渠道名稱
        public decimal Timezone { get; set; } //時區
        public string Status { get; set; }//PENDDING/FAIL/SUCCESS
        public string OrderType { get; set; }//W=出款/D=充值
    }

    public class Withdrawal
    {
        public string WithdrawSerial { get; set; } //訂單號
        public DateTime CreateDate { get; set; } //訂單建立時間
        public string CompanyName { get; set; }  //商戶名稱
        public string CompanyCode { get; set; }  //商戶代碼
        public string CurrencyType { get; set; } //幣別
        public decimal Amount { get; set; } //訂單金額
        public int Status { get; set; } //流程狀態，0=建立/1=進行中/2=成功/3=失敗
        public string DownOrderID { get; set; } //商戶訂單號
        public int IsBlockChainOrder { get; set; } //0=銀行卡出款 /1= 區塊鏈出款
        public decimal Timezone { get; set; } //時區

        public string BankCard { get; set; } //銀行卡號 或 區塊鏈地址
        //銀行卡出款相關
        public string BankCardName { get; set; } //持卡人姓名
        public string BankName { get; set; } // 銀行名稱
        public string BankBranchName { get; set; } // 支行
        public string ProviderName { get; set; } //供應商名稱
        public string ServiceTypeName { get; set; } //渠道名稱
        public decimal CollectCharge { get; set; }//手續費(商户)
        public decimal CostCharge { get; set; }//手續費(供應商)
        //區塊鏈出款相關
        public decimal UpBlockChainRate { get; set; } //供應商浮動馬數
        public decimal DownBlockChainRate { get; set; }//商戶浮動馬數
        public decimal OKEXRate { get; set; } //區塊鏈費率
        public decimal DownBlockChainAmount { get; set; } //商戶出款 U 數
        public decimal UpBlockChainAmount { get; set; } //供應商出款 U 數
        public decimal UpRate { get; set; }  //上游費率(%)
        public decimal DownRate { get; set; } //下游費率(%)
    }

    public class Deposite
    {
        public string PaymentSerial { get; set; } //訂單號
        public string CurrencyType { get; set; }  //訂單建立時間
        public string CompanyName { get; set; }  //商戶名稱
        public string CompanyCode { get; set; }  //商戶代碼
        public string ProviderName { get; set; } //供應商名稱
        public decimal Amount { get; set; } //訂單金額
        public DateTime FinishDate { get; set; } //訂單完成時間
        public DateTime CreateDate { get; set; } //訂單建立時間
        public decimal CostRate { get; set; } //供應商費率
        public decimal CostCharge { get; set; }//供應商固定手續費
        public decimal CollectRate { get; set; }//商戶費率
        public decimal CollectCharge { get; set; } //商戶固定手續費
        public string ServiceTypeName { get; set; }//渠道名稱
        public decimal Timezone { get; set; } //時區
        public int ProcessStatus { get; set; }//0=新建(準備 PaymentSerial)/1=尚未提交/2=交易成功，尚未通知商戶/3=交易失敗/4=交易成功，通知商戶成功

    }

    public class SummaryCompanyByDate
    {
        public DateTime SummaryDate { get; set; } //統計日期
        public string ServiceTypeName { get; set; }//渠道名稱
        public string CompanyName { get; set; }  //商戶名稱
        public decimal CompanyPoint { get; set; }  //商戶當前餘額
        public decimal SummaryAmount { get; set; } //充值總額
        public decimal SummaryWithdrawalAmount { get; set; }  //出款總額
        public decimal CompanyBalance { get; set; } //商戶當前餘額
        public int TotalCount { get; set; } //充值訂單總數
        public int SuccessCount { get; set; } //充值完成訂單總數
    }

    public class BlockChainRateAPIResult : APIResult
    {
        public List<BlockChainRate> BlockChainRates;
    }

    public class BlockChainRate
    {
        public string CurrencyType { get; set; } //幣別
        public decimal? Rate { get; set; } //匯率
        public decimal? UpFloatRate { get; set; } //上浮碼數
        public decimal? DownFloatRate { get; set; } //下浮碼數
    }

    #region Result

    public class APIResult {
        public ResultStatus Status;
        public string Message;
    }

    public enum ResultStatus {
        OK = 0,
        ERR = 1,
        SignErr = 2,
        Invalidate = 3,
        Success=4
    }

    public class PaymentAccountingResult : APIResult {
        public string CompanyCode;
        public string PaymentSerial;
        public int ProcessStatus;
        public string CurrencyType;
        public decimal OrderAmount;
        public decimal PaymentAmount;
        public GatewayCommon.PaymentByProvider PaymentByProvider;

    }

    public class AutoDistributionGroupWithdrawData : APIResult
    {
        public List<string> WithdrawSerials;
    }
    

    public class ProviderBalanceResult : APIResult {
        public List<ProviderBalance> ArrayProviderBalance { get; set; }
    }

    public class GetProviderPointListResult : APIResult
    {
        public List<FromBodyQueryProviderPoint> Providers { get; set; }
    }

    public class QueryProviderPointResult : APIResult {
        public GatewayCommon.BalanceByProvider BalanceData { get; set; }
    }

  

    public class ProviderBalance {
        public string ProviderCode { get; set; }
        public string CurrencyType { get; set; }
        //帳戶總餘額
        public decimal AccountBalance { get; set; }
        //可用餘額
        public decimal CashBalance { get; set; }
        public bool IsProviderSupport { get; set; }
        public string ProviderReturn { get; set; }
    }

    public class WithdrawResult : APIResult {
        // 0=即時/1=非即時
        public int SendType;
        public string WithdrawSerial;
        public string UpOrderID;
        public int SendStatus;
        public decimal DidAmount;
        public decimal Balance;
    }
    public class MsgList {
        public List<Msg> List = new List<Msg>();
    }

    public class Msg {
        public int MsgID { set; get; }
        public string MsgContent { set; get; }
    }

    #endregion

    #region FromBody

    public class TigerPayWithdrawData
    {
        public string result { get; set; }
        public string status { get; set; }
        public string transaction_number { get; set; }
        public string currency { get; set; }
        public string amount { get; set; }
        public string fee { get; set; }
    }

    public class FromBodyQueryWithdrawal
    {
        public List<int> LstWithdrawID { get; set; }
        public string Sign { get; set; }
    }

    public class FromBodyBalance {
        public List<string> ArrayProviderCode { get; set; }
        public string CurrencyType { get; set; }
        public string Sign { get; set; }
    }

    public class FromBodyQueryProviderPoint
    {
        public string CurrencyType { get; set; }
        public string ProviderCode { get; set; }
        public string Sign { get; set; }
    }

public class FromBodyQueryPointByPayment {
        public string CompanyCode { get; set; }
        public string PaymentSerial { get; set; }
        public string Sign { get; set; }
    }

    public class FromBodyRequireWithdrawal {
        public string WithdrawSerial { get; set; }
        public string Sign { get; set; }
    }

    public class FromBodyReSendPayment
    {
        public string PaymentSerial { get; set; }
        public string Sign { get; set; }
    }

    public class FromBodyReSendWithdraw
    {
        public string WithdrawSerial { get; set; }
        public string Sign { get; set; }
        public bool isReSendWithdraw { get; set; }
    }

    public class FromBodySendWithdrawal
    {
        public string WithdrawSerial { get; set; }
        public string Sign { get; set; }

    }

    public class FromBodyGetOrder
    {
        public string CompanyCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string OrderNumber { get; set; }
        public DateTime QueryDate { get; set; }
    }

    public class FromBodyReview
    {
        public string WithdrawSerial { get; set; }
        public string PaymentSerial { get; set; }
        public decimal RealAmount { get; set; }
        public int Status { get; set; }
    }

    public class FromBodyTest {
        public string CurrencyType { get; set; }
        public string CompanyCode { get; set; }
        public string PaymentSerial { get; set; }
        public string OrderID { get; set; }
        public string OrderAmount { get; set; }
        public string OrderDate { get; set; }
        public string ServiceType { get; set; }
        public int Type { get; set; }
    }

    #endregion

}
