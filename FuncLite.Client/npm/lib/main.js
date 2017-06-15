#! /usr/bin/env node

var path = require('path');
var fs = require('fs');
var spawn = require('child_process').spawn;
var fork = require('child_process').fork;
var args = process.argv;

function main() {
  var isWin = /^win/.test(process.platform);
  var bin = path.join(path.dirname(fs.realpathSync(__filename)), '../../bin/Release/PublishOutput');

  if (!isWin) {
      var funcProc = spawn("mono " + bin + '/FuncLite.Client.exe', args.slice(2), { stdio: [process.stdin, process.stdout, process.stderr, 'pipe'] });
      funcProc.on('exit', function (code) {
          process.exit(code);
      });
  } else {
      var funcProc = spawn(bin + '/FuncLite.Client.exe', args.slice(2), { stdio: [process.stdin, process.stdout, process.stderr, 'pipe'] });
      funcProc.on('exit', function (code) {
          process.exit(code);
      });
  }
}

main();