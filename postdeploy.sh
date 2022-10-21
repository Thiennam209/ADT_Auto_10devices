iothubname=$1
adtname=$2
rgname=$3
location=$4
egname=$5
egid=$6
funcappid=$7
storagename=$8
containername=$9



echo "iot hub name: ${iothubname}"
echo "adt name: ${adtname}"
echo "rg name: ${rgname}"
echo "location: ${location}"
echo "egname: ${egname}"
echo "egid: ${egid}"
echo "funcappid: ${funcappid}"
echo "storagename: ${storagename}"
echo "containername: ${containername}"


# echo 'installing azure cli extension'
az config set extension.use_dynamic_install=yes_without_prompt
az extension add --name azure-iot -y

# echo 'retrieve files'
git clone https://github.com/Thiennam209/ARM-Template-WTA.git

# echo 'input model'
autoid=$(az dt model create -n $adtname --models ./ARM-Template-WTA/models/auto.json --query [].id -o tsv)

# echo 'instantiate ADT Instances'
for i in {1..10}
do
    echo "Create auto autoid$i"
    az dt twin create -n $adtname --dtmi $autoid --twin-id "autoid$i"
    az dt twin update -n $adtname --twin-id "autoid$i" --json-patch '[{"op":"add", "path":"/autoid", "value": "'"autoid$i"'"}]'
done

# az eventgrid topic create -g $rgname --name $egname -l $location
az dt endpoint create eventgrid --dt-name $adtname --eventgrid-resource-group $rgname --eventgrid-topic $egname --endpoint-name "$egname-ep"
az dt route create --dt-name $adtname --endpoint-name "$egname-ep" --route-name "$egname-rt"

# Create Subscriptions
az eventgrid event-subscription create --name "$egname-broadcast-sub" --source-resource-id $egid --endpoint "$funcappid/functions/broadcast" --endpoint-type azurefunction

# Retrieve and Upload models to blob storage
az storage blob upload-batch --account-name $storagename -d $containername -s "./ARM-Template-WTA/assets"