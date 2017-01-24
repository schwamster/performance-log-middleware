# health-check-middleware

This is simple piece of asp.net core middleware that a monitoring service can call to determine if the app is alive.
You will be able to supply custom metrics to give the caller more detailed information about the service/app.

## Getting started

### Install the package
Install the nuget package from [nuget](https://www.nuget.org/packages/health-check-middleware/)

Either add it with the PM-Console:
        
        Install-Package health-check-middleware

Or add it to project.json
        "dependencies": {
            ...
            "health-check-middleware": "XXX"
        }

### Set your api up

Edit your Startup.cs -> 

        Configure(){
            ...

            app.UseHealthcheckEndpoint(new HealthCheckOptions() { Message = "Its alive!" });
            
            ...
        }


Thats it now you can start your Api and navigate to http://localhost:<randomport>/healthcheck

The result should look like this




## Build and Publish
The package is build in docker so you will need to install docker to build and publish the package.
(Of course you could just build it on the machine you are running on and publish it from there. 
I prefer to build and publish from docker images to have a reliable environment, plus make it easier 
to build this on circleci).

### build

run:
        docker-compose -f docker-compose-build.yml up

this will build & test the code. The testresult will be in folder ./testresults and the package in ./package

### publish

run (fill in the api key):

        docker run --rm -v ${PWD}/package:/data/package schwamster/nuget-docker push /data/package/*.nupkg <your nuget api key> -Source nuget.org

this will take the package from ./package and push it to nuget.org