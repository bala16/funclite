const http = require("http");
const url = require('url');

var userFunc;

http.createServer(function (request, response) {
    console.log('Got request for ' + request.url + ' at ' + (new Date()).toLocaleTimeString());

    var queryData = url.parse(request.url, true).query;

    if (request.method == 'GET'){
        ResponseEnd(response, {
            message: 'executed warmup request',
            nodeVersion: process.version
        });
        return;
    }

    var body = [];
    request.on('data', function(chunk) {
        body.push(chunk);
    }).on('end', function() {
        var jsonBody = JSON.parse(Buffer.concat(body).toString());

        if (typeof userFunc == 'undefined') {
            console.log('Loading user function and injecting the environment');
            userFunc = require(process.env.funcFile);

            for (var varName in jsonBody.environment) {
                process.env[varName] = jsonBody.environment[varName];
            }
        }

        var logLines = [];
        var context = {
            log: function(s) {
                logLines.push(s);
            },
            done: function () {
                ResponseEnd(response, {
                    functionBody: context.res.body,
                    logs: logLines
                });
            }
        };

        var childreq = {
            query: queryData,
            body: jsonBody.functionBody
        };

        userFunc(context, childreq);
    });

}).listen(process.env.PORT || 8080);

function ResponseEnd(response, o) {
    response.writeHead(200, {'Content-Type': 'application/json'});
    response.end(JSON.stringify(o));
}