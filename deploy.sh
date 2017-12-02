# Continous deployment script
cd ~/formatik/formatik-payment/
git remote update

PAYMENT-LOCAL=$(git rev-parse @)
PAYMENT-REMOTE=$(git rev-parse @{u})
PAYMENT-BASE=$(git merge-base @ @{u})

if [[ $LIB_LOCAL = $LIB_REMOTE && $TEST_LOCAL = $TEST_REMOTE && $PAYMENT-LOCAL = $PAYMENT-REMOTE && $1 != "force" ]]; then
    echo "Up-to-date"
elif [[ $LIB_LOCAL = $LIB_BASE || $TEST_LOCAL = $TEST_BASE || $PAYMENT-LOCAL = $PAYMENT-BASE || $1 == "force" ]]; then
    echo "Rebuilding..."
    
    cd formatik-payment/
    git pull

    # Restores need to be executed in every container. 
    # Restores from prior containers or the host are not valid inside a new container
    
    #run unit Tests
    echo "Building Payment..."
    docker run \
        --rm \
        -v ~/formatik/formatik-payment:/var/formatik-payment \
        -w /var/formatik-payment \
        -c 512 \
        microsoft/dotnet:2.0.3-sdk \
        /bin/bash -c "rm -r bin; rm -r obj; dotnet restore; dotnet publish -c release /property:PublishWithAspNetCoreTargetManifest=false"

    sudo chmod o+rw -R bin

    echo "...Build complete"

    echo "Building new Payment Docker image..."
    cp Dockerfile bin/release/netcoreapp2.0/publish/

    cd bin/release/netcoreapp2.0/publish/
    
    docker rmi octagon.formatik.payment:old
    docker tag octagon.formatik.Payment:latest octagon.formatik.payment:old
    docker build --tag=octagon.formatik.payment:latest .

    echo "...image build complete"

    echo "Updating Payment service..."

    # For new swarms create service manually like this
    # docker service create \
    #     --network formatik_net \
    #     --replicas 1 \
    #     --constraint 'node.labels.payment == true' \
    #     --name Payment \
    #     --hostname formatik-payment \
    #     octagon.formatik.payment:latest

    #docker run --rm -ti --name Payment-test octagon.formatik.payment:latest

    docker service update \
        --image octagon.formatik.payment:latest \
        --force \
        Payment

    echo "...Payment service updated"

    curl -s --user 'api:key-0f66fb27e1d888d8d5cddaea7186b634' \
        https://api.mailgun.net/v3/sandboxf5c90e4cf7524486831c10e8d6475ebd.mailgun.org/messages \
            -F from='Formatik01 <postmaster@sandboxf5c90e4cf7524486831c10e8d6475ebd.mailgun.org>' \
            -F to='Bobby Kotzev <bobby@octagonsolutions.co>' \
            -F subject='Successfully updated Formatik Payment' \
            -F text='...using latest source from master branch'
fi

