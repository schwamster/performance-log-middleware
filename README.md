# health-check-middleware

This is simple piece of asp.net core middleware that adds a performance times the duration to each incoming request and logs
it to the configured Logger (ILoggerFactory)

## Getting started

### Install the package
Install the nuget package from [nuget](https://www.nuget.org/packages/performance-log-middleware/)

Either add it with the PM-Console:
        
        Install-Package performance-log-middleware

Or add it to project.json
        "dependencies": {
            ...
            "performance-log-middleware": "XXX"
        }

### Set your api up

Edit your Startup.cs -> 

        Configure(){
            ...

            app.UsePerformanceLog(new PerformanceLogOptions());
            
            ...
        }


Thats it now you application logs all request durations to your configured logger.

### Options

PerformanceLogOptions

none yet


## Build and Publish
The package is build in docker so you will need to install docker to build and publish the package.
(Of course you could just build it on the machine you are running on and publish it from there. 
I prefer to build and publish from docker images to have a reliable environment, plus makes it easier 
to build this on circleci).

### build

run:
        docker-compose -f docker-compose-build.yml up

this will build & test the code. The testresult will be in folder ./testresults and the package in ./package

### publish

run (fill in the api key):

        docker run --rm -v ${PWD}/package:/data/package schwamster/nuget-docker push /data/package/*.nupkg <your nuget api key> -Source nuget.org

this will take the package from ./package and push it to nuget.org