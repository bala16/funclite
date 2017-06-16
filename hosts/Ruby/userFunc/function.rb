
def func(context, doneFunc, &logFunc)

	logFunc.call('Ruby HTTP trigger function processed a request.')

	routeParams = context[:routeParams]
	functionOwner = ""

	if context[:settings].key?("FunctionOwner")
		functionOwner = context[:settings]["FunctionOwner"]
	end

	if (routeParams[:name])
		context[:response][:status] = 200
		context[:response][:body] = "Hello " + routeParams[:name] + " from " + functionOwner
	else
		if (context[:jsonBody].key?("functionBody") && context[:jsonBody]["functionBody"].key?("name"))
			context[:response][:status] = 200
			context[:response][:body] = "Hello " + context[:jsonBody]["functionBody"]["name"] + " from " + functionOwner
		else
			context[:response][:status] = 400
			context[:response][:body] = "Please pass a name on the query string or in the request body"
		end
	end
	
		
	doneFunc.call(context)
end
