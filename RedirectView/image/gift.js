var PriceType_FaceValueNotExist = 1;
var Gift = {
    calculateRate: function(value, rate) {
        return Math.ceil(value * Math.floor(rate * 100) / 10000);
    }
};