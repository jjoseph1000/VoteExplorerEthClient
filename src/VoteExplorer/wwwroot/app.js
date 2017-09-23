var accounts;
var account;
var foodSafeABI;
var foodSafeContract;
var foodSafeCode;
window.App = {
  start: function() {
    var self = this;
    web3.eth.getAccounts(function(err, accs) {
      if (err != null) {
        alert("There was an error fetching your accounts.");
        return;
      }
      if (accs.length == 0) {
        alert("Couldn't get any accounts! Make sure your Ethereum client is configured correctly.");
        return;
      }

      accounts = accs;
      account = accounts[0];
      web3.eth.defaultAccount= account;
      var foodSafeSource= "pragma solidity ^0.4.13;  contract tokenRecipient { function receiveApproval(address _from, uint256 _value, address _token, bytes _extraData); }  contract MyToken { /* Public variables of the token */ string public name; string public symbol; uint8 public decimals; uint256 public totalSupply;  /* This creates an array with all balances */ mapping (address => uint256) public balanceOf; mapping (address => mapping (address => uint256)) public allowance;  /* This generates a public event on the blockchain that will notify clients */ event Transfer(address indexed from, address indexed to, uint256 value);  /* This notifies clients about the amount burnt */ event Burn(address indexed from, uint256 value);  /* Initializes contract with initial supply tokens to the creator of the contract */ function MyToken( ) { balanceOf[msg.sender] = 100000000;              // Give the creator all initial tokens totalSupply = 100000000;                        // Update total supply name = \"My wonderful Token\";                                   // Set the name for display purposes symbol = \"ABC\";                               // Set the symbol for display purposes decimals = 0;                            // Amount of decimals for display purposes }  /* Internal transfer, only can be called by this contract */ function _transfer(address _from, address _to, uint _value) internal { require (_to != 0x0);                               // Prevent transfer to 0x0 address. Use burn() instead require (balanceOf[_from] > _value);                // Check if the sender has enough require (balanceOf[_to] + _value > balanceOf[_to]); // Check for overflows balanceOf[_from] -= _value;                         // Subtract from the sender balanceOf[_to] += _value;                            // Add the same to the recipient Transfer(_from, _to, _value); }  /// @notice Send `_value` tokens to `_to` from your account /// @param _to The address of the recipient /// @param _value the amount to send function transfer(address _to, uint256 _value) { _transfer(msg.sender, _to, _value); }  /// @notice Send `_value` tokens to `_to` in behalf of `_from` /// @param _from The address of the sender /// @param _to The address of the recipient /// @param _value the amount to send function transferFrom(address _from, address _to, uint256 _value) returns (bool success) { require (_value < allowance[_from][msg.sender]);     // Check allowance allowance[_from][msg.sender] -= _value; _transfer(_from, _to, _value); return true; }  /// @notice Allows `_spender` to spend no more than `_value` tokens in your behalf /// @param _spender The address authorized to spend /// @param _value the max amount they can spend function approve(address _spender, uint256 _value) returns (bool success) { allowance[msg.sender][_spender] = _value; return true; }  /// @notice Allows `_spender` to spend no more than `_value` tokens in your behalf, and then ping the contract about it /// @param _spender The address authorized to spend /// @param _value the max amount they can spend /// @param _extraData some extra information to send to the approved contract function approveAndCall(address _spender, uint256 _value, bytes _extraData) returns (bool success) { tokenRecipient spender = tokenRecipient(_spender); if (approve(_spender, _value)) { spender.receiveApproval(msg.sender, _value, this, _extraData); return true; } }         /// @notice Remove `_value` tokens from the system irreversibly /// @param _value the amount of money to burn function burn(uint256 _value) returns (bool success) { require (balanceOf[msg.sender] > _value);            // Check if the sender has enough balanceOf[msg.sender] -= _value;                      // Subtract from the sender totalSupply -= _value;                                // Updates totalSupply Burn(msg.sender, _value); return true; }  function burnFrom(address _from, uint256 _value) returns (bool success) { require(balanceOf[_from] >= _value);                // Check if the targeted balance is enough require(_value <= allowance[_from][msg.sender]);    // Check allowance balanceOf[_from] -= _value;                         // Subtract from the targeted balance allowance[_from][msg.sender] -= _value;             // Subtract from the sender's allowance totalSupply -= _value;                              // Update totalSupply Burn(_from, _value); return true; } }";
      web3.eth.compile.solidity(foodSafeSource, function(error, foodSafeCompiled){
        console.log(error);
        console.log(foodSafeCompiled);
   
       if (foodSafeCompiled != null)
          {
            foodSafeABI = foodSafeCompiled['<stdin>:FoodSafe'].info.abiDefinition;
            document.getElementById("ABI").value = JSON.stringify(foodSafeCompiled['<stdin>:tokenRecipient'].info.abiDefinition);
            foodSafeContract = web3.eth.contract(foodSafeABI);
            foodSafeCode = foodSafeCompiled['<stdin>:FoodSafe'].code
            document.getElementById("byteCode").value = foodSafeCompiled['<stdin>:tokenRecipient'].code;
          }
      else
        {
            ABI = "[{\"constant\":false,\"inputs\":[{\"name\":\"LocationId\",\"type\":\"uint256\"},{\"name\":\"Name\",\"type\":\"string\"},{\"name\":\"Secret\",\"type\":\"string\"}],\"name\":\"AddNewLocation\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"GetTrailCount\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"TrailNo\",\"type\":\"uint8\"}],\"name\":\"GetLocation\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"},{\"name\":\"\",\"type\":\"uint256\"},{\"name\":\"\",\"type\":\"uint256\"},{\"name\":\"\",\"type\":\"uint256\"},{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"type\":\"function\"}]";
            foodSafeCode = "0x60606040526001805460ff19169055341561001957600080fd5b5b6105b8806100296000396000f300606060405263ffffffff7c0100000000000000000000000000000000000000000000000000000000600035041663988bbca38114610053578063bbe42af8146100ed578063e3fd1ec214610116575b600080fd5b341561005e57600080fd5b6100eb600480359060446024803590810190830135806020601f8201819004810201604051908101604052818152929190602084018383808284378201915050505050509190803590602001908201803590602001908080601f01602080910402602001604051908101604052818152929190602084018383808284375094965061022795505050505050565b005b34156100f857600080fd5b6101006102f3565b60405160ff909116815260200160405180910390f35b341561012157600080fd5b61012f60ff600435166102fd565b604051808060200186815260200185815260200184815260200180602001838103835288818151815260200191508051906020019080838360005b838110156101835780820151818401525b60200161016a565b50505050905090810190601f1680156101b05780820380516001836020036101000a031916815260200191505b50838103825284818151815260200191508051906020019080838360005b838110156101e75780820151818401525b6020016101ce565b50505050905090810190601f1680156102145780820380516001836020036101000a031916815260200191505b5097505050505050505060405180910390f35b61022f61048d565b828152602081018490526080810182905242606082015260015460ff1615610271576001805460ff166000908152602081905260409081902090910154908201525b60015460ff166000908152602081905260409020819081518190805161029b9291602001906104c8565b506020820151816001015560408201518160020155606082015181600301556080820151816004019080516102d49291602001906104c8565b50506001805460ff80821683011660ff19909116179055505b50505050565b60015460ff165b90565b610305610547565b6000806000610312610547565b60ff861660009081526020818152604091829020600180820154600280840154600385015485549597939691959094600489019489949183161561010002600019019092160491601f8301819004810201905190810160405280929190818152602001828054600181600116156101000203166002900480156103d65780601f106103ab576101008083540402835291602001916103d6565b820191906000526020600020905b8154815290600101906020018083116103b957829003601f168201915b50505050509450808054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156104725780601f1061044757610100808354040283529160200191610472565b820191906000526020600020905b81548152906001019060200180831161045557829003601f168201915b50505050509050945094509450945094505b91939590929450565b60a0604051908101604052806104a1610547565b81526020016000815260200160008152602001600081526020016104c3610547565b905290565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061050957805160ff1916838001178555610536565b82800160010185558215610536579182015b8281111561053657825182559160200191906001019061051b565b5b5061054392915061056b565b5090565b60206040519081016040526000815290565b60206040519081016040526000815290565b6102fa91905b808211156105435760008155600101610571565b5090565b905600a165627a7a72305820c54cc677e9fecf955b0d4770e9ba5b1c96c645ba50e2a28f004339cabc297bc50029";
           
            foodSafeABI = JSON.parse(ABI);            
            document.getElementById("ABI").value = JSON.stringify(foodSafeABI);
            foodSafeContract = web3.eth.contract(foodSafeABI);
            document.getElementById("byteCode").value = foodSafeCode;    
        }

      });
    });
  },
    createContract: function()
  {
    foodSafeContract.new("", {from:account, data: foodSafeCode, gas: 3000000}, function (error, deployedContract){
      if(deployedContract.address)
      {
        document.getElementById("contractAddress").value=deployedContract.address;
      }
    })
  },
  addNewLocation: function()
  {
    var contractAddress = document.getElementById("contractAddress").value;
    var deployedFoodSafe = foodSafeContract.at(contractAddress);
    var locationId = document.getElementById("locationId").value;
    var locationName = document.getElementById("locationName").value;
    var locationSecret = document.getElementById("secret").value;
    var passPhrase = document.getElementById("passPhrase").value;
    var encryptedSecret = CryptoJS.AES.encrypt(locationSecret,passPhrase).toString();
    deployedFoodSafe.AddNewLocation(locationId, locationName, encryptedSecret, function(error){
      console.log(error);
    })
  },
  getCurrentLocation: function()
  {
    var contractAddress = document.getElementById("contractAddress").value;
    var deployedFoodSafe = foodSafeContract.at(contractAddress);
    var passPhrase = document.getElementById("passPhrase").value;
    deployedFoodSafe.GetTrailCount.call(function (error, trailCount){
      deployedFoodSafe.GetLocation.call(trailCount-1, function(error, returnValues){
        document.getElementById("locationId").value= returnValues[1];
        document.getElementById("locationName").value = returnValues[0];
        var encryptedSecret = returnValues[4];
        var decryptedSecret = CryptoJS.AES.decrypt(encryptedSecret, passPhrase).toString(CryptoJS.enc.Utf8);
        document.getElementById("secret").value=decryptedSecret;
      })
    })    
  }
};

window.addEventListener('load', function() {
  if (typeof web3 !== 'undefined') {
    console.warn("Using web3 detected from external source.  If using MetaMask, see the following link. Feel free to delete this warning. :) http://truffleframework.com/tutorials/truffle-and-metamask")
    window.web3 = new Web3(web3.currentProvider);
  } else {
    console.warn("No web3 detected. Falling back to http://localhost:8545. You should remove this fallback when you deploy live, as it's inherently insecure. Consider switching to Metamask for development. More info here: http://truffleframework.com/tutorials/truffle-and-metamask");
    // fallback - use your fallback strategy (local node / hosted node + in-dapp id mgmt / fail)
    window.web3 = new Web3(new Web3.providers.HttpProvider("http://localhost:8545"));
  }
  App.start();
});
