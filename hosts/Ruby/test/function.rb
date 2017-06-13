
def func(context, doneFunc, &logFunc)

	logFunc.call('Ruby HTTP trigger function processed a request.')

	routeParams = context[:routeParams]

	if routeParams['name']
		context[:response][:status] = 200
		context[:response][:body] = "Hello " + routeParams['name'] + " from " + settings.env[:FunctionOwner]
	else
		context[:response][:status] = 400
		context[:response][:body] = "Please pass a name on the query string or in the request body"
	end

	doneFunc.call(context)
end