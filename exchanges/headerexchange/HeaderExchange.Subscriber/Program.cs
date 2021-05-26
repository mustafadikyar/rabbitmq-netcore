﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HeaderExchange.Subscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionFactory factory = new(); //rabbitmq için bir bağlantı örneği oluşturduk.
            factory.Uri = new Uri("amqps://gpgmtthl:dtDiiqd4qyog_b3f7WjHYjVWlYvai-ff@baboon.rmq.cloudamqp.com/gpgmtthl"); //bağlantı adresi

            using IConnection connection = factory.CreateConnection(); //bağlantı açıyoruz.
            using IModel channel = connection.CreateModel(); //kanal oluşturuyoruz.

            //publisher tarafı çalışmadan (header bilgisi oluşmadan) consumer çalışırsa hatayı engellemek adına.
            //olumsuz bir durum değil var ise işlem yapılmayacak yok ise bir exchange oluşturulacak.
            channel.ExchangeDeclare(exchange: "header-exchange", type: ExchangeType.Headers, durable: false, autoDelete: false, arguments: null);

            //mesajlara erişim kıstasları
            //prefetchSize : mesaj boyutu (0 : herhangi bir boyuttaki mesaj)
            //prefetchCount : mesajlar tüketicilere kaçar kaçar gönderilecek.
            //global :  toplamda tüm kullanıcılara kaçar kaçar gönderilecek. 3 consumer ve 6 mesaj için her consumer'a ikişer tane mesaj gönderir.
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            //tüketici örneği oluşturuyoruz. 
            EventingBasicConsumer consumer = new(channel);
            var queueName = channel.QueueDeclare().QueueName;

            Dictionary<string, object> headers = new();
            headers.Add("format", "pdf");
            headers.Add("shape", "a4");
            //all : header bilgisi birebir uymak zorunda | any
            headers.Add("x-match", "all");

            channel.QueueBind(queueName, "header-exchange", routingKey: string.Empty, arguments: headers);

            //queue : izlenecek kuyruk
            //autoAck : (true : mesaj işlendiği zaman (başarılı başarısız farketmez) kuyruktan silinir. | false : işlem başarılıysa kuyruktan silmesini biz bildiriyoruz.)
            //consumer : tüketici
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            Console.WriteLine("Mesaj bekleniyor...");

            //mesajı dinleyen event.
            consumer.Received += (object sender, BasicDeliverEventArgs e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray()); //yakalanan mesaj
                Console.WriteLine($"Gelen mesaj : {message}");

                //işlemin kuyruktan silinmesi için haber yolluyoruz.
                //deliveryTag : gelen mesajın adresi
                //multiple : eğer kuyrukta işlem tamamlanmış fakat silinmemiş işlemler var ise onları siler.
                channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
            };
            Console.ReadLine();
        }
    }
}