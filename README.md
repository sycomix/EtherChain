# Ether chain

Explorer ethereum block chain to get transactions and calculate the balances.
Uses dot net core 2.2.101

## AppSettings.cs

Change the values there for your custom needs.

## How to install

This program is based on Ethereum ETL so we need to install it first.

  - Install python 3 https://www.python.org/ftp/python/3.7.3/python-3.7.3-amd64.exe
  - Install VS c++ 14 build tools https://download.microsoft.com/download/9/3/F/93FCF1E7-E6A4-478B-96E7-D4B285925B00/vc_redist.x64.exe
  - git submodule update --init
  - go to the deps\ethereum-etl directory
  - run pip3 install -e .

  python ethereumetl.py export_token_transfers --start-block 2000000 --end-block 2000000 --provider-uri wss://mainnet.infura.io/ws --output token_transfers.csv -w 1