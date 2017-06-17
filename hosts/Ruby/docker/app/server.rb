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

	require_relative settings.funcFile

	jsonBody = Hash.new

	request.body.rewind
	requestBody = request.body.read.to_s
	if !requestBody.empty?
		jsonBody = JSON.parse requestBody
	end

	settingsFileContents = File.read(File.join(File.dirname(__FILE__), settings.settingsFile))
	settings = JSON.parse settingsFileContents

	context = Hash.new
	context[:response] = Hash.new
	context[:routeParams] = params
	context[:jsonBody] = jsonBody
	context[:settings] = settings

	logs = Array.new
	logFunc = lambda do |logLine|
		logs.push(logLine)
	end

	doneFunc = Proc.new do |context|
		halt context[:response][:status], {:functionBody => context[:response][:body], :logs => logs}.to_json
	end

	func(context, doneFunc, &logFunc)
end
