# My API 

## Getting Started 
To build the SDKs for My API, simply install AutoRest via `npm` (`npm install -g autorest`) and then run:
> `autorest readme.md`

To see additional help and options, run:
> `autorest --help`

For other options on installation see [Installing AutoRest](https://aka.ms/autorest/install) on the AutoRest github page.

---

## Configuration 
The following are the settings for this using this API with AutoRest.

``` yaml
# specify the version of Autorest to use
version: latest-release
input-file:
    - funclite-swagger.json

csharp:
    - output-folder: generated
	namespace: FuncLite.Client.BackendHelper

# (more settings here...)
```