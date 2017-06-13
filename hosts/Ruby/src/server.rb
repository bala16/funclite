require 'sinatra'
require 'sinatra/config_file'
require 'json'

config_file './config.yml'

configure do
	set :loaded, false
	set :env, Hash.new
end

before do
	content_type 'application/json'
end

get '*' do
	halt 200, {message: "executed warmup request"}.to_json
end

post '*' do

	jsonBody = JSON.parse(request.body.read, :symbolize_names => true)

	if (!settings.loaded)
		settings.loaded = true
		jsonBody[:environment].each do |key,value|
			settings.env[key] = value
		end
	end

	require_relative settings.funcFile

	context = Hash.new
	context[:response] = Hash.new
	context[:routeParams] = params

	logs = Array.new
	logFunc = lambda do |logLine|
		logs.push(logLine)
	end

	doneFunc = Proc.new do |context|
		context[:response][:logs] = logs
		halt context[:response][:status], JSON.generate(context[:response])
	end
	
	func(context, doneFunc, &logFunc)
end
